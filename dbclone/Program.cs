using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddSingleton<PostgresCloneRunner>();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

try
{
    Env.Load();
    Env.Load("../.env");

    var runner = app.Services.GetRequiredService<PostgresCloneRunner>();
    await runner.RunAsync(CancellationToken.None);
    return 0;
}
catch (Exception exception)
{
    logger.LogError(exception, "Falha durante a clonagem do banco de dados.");
    return 1;
}

internal sealed class PostgresCloneRunner(ILogger<PostgresCloneRunner> logger)
{
    private static readonly Regex CreationRegex = new(@"pg_restore:\s+creating\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TableDataRegex = new(@"TABLE DATA\s+(.+?)\s+(.+?)\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DataProcessingRegex = new(@"pg_restore:\s+processing data for table\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var settings = CloneSettings.LoadFromEnvironment();

        logger.LogInformation("Iniciando clonagem do banco '{SourceDatabase}' para '{TargetDatabase}'.", settings.Source.Database, settings.Target.Database);

        await EnsureCommandExistsAsync("pg_dump", cancellationToken);
        await EnsureCommandExistsAsync("pg_restore", cancellationToken);

        var temporaryDirectory = CreateTemporaryDirectory();
        logger.LogInformation("Diretorio temporario criado em '{TemporaryDirectory}'.", temporaryDirectory);

        try
        {
            var dumpPath = Path.Combine(temporaryDirectory, $"{settings.Source.Database}.dump");
            await CreateDumpAsync(settings.Source, dumpPath, cancellationToken);

            var tableCounts = await ReadSourceTableCountsAsync(settings.Source, dumpPath, cancellationToken);
            await RecreateTargetDatabaseAsync(settings.Target, cancellationToken);

            await RestoreSectionAsync(
                settings.Target,
                dumpPath,
                "pre-data",
                tableCounts,
                logDataStartsOnly: false,
                cancellationToken: cancellationToken);

            await RestoreSectionAsync(
                settings.Target,
                dumpPath,
                "data",
                tableCounts,
                logDataStartsOnly: true,
                cancellationToken: cancellationToken);

            await RestoreSectionAsync(
                settings.Target,
                dumpPath,
                "post-data",
                tableCounts,
                logDataStartsOnly: false,
                cancellationToken: cancellationToken);

            logger.LogInformation("Clonagem concluida com sucesso.");
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
                logger.LogInformation("Diretorio temporario removido: '{TemporaryDirectory}'.", temporaryDirectory);
            }
        }
    }

    private async Task EnsureCommandExistsAsync(string command, CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(
            new ProcessStartInfo
            {
                FileName = command,
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            },
            line => logger.LogDebug("{Command}: {Line}", command, line),
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"O comando '{command}' nao esta disponivel. Instale os utilitarios cliente do PostgreSQL antes de executar a clonagem.");
        }
    }

    private string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rendatop-dbclone-{DateTime.UtcNow:yyyyMMddHHmmss}");
        Directory.CreateDirectory(path);
        return path;
    }

    private async Task CreateDumpAsync(DatabaseSettings source, string dumpPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Gerando dump temporario do banco de origem '{Database}' em '{DumpPath}'.", source.Database, dumpPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "pg_dump",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("--format=custom");
        startInfo.ArgumentList.Add("--compress=0");
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("--no-privileges");
        startInfo.ArgumentList.Add($"--file={dumpPath}");
        startInfo.ArgumentList.Add(source.Database);
        ConfigurePostgresEnvironment(startInfo, source);

        var result = await RunProcessAsync(
            startInfo,
            line => logger.LogInformation("pg_dump: {Line}", line),
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Nao foi possivel gerar o dump do banco de origem.");
        }

        logger.LogInformation("Dump gerado com sucesso.");
    }

    private async Task<Dictionary<string, long>> ReadSourceTableCountsAsync(DatabaseSettings source, string dumpPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Lendo a lista de tabelas com dados e calculando a quantidade de tuplas na origem.");

        var tables = await ReadDumpTablesAsync(source, dumpPath, cancellationToken);
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);

        await using var connection = new NpgsqlConnection(source.ToConnectionString());
        await connection.OpenAsync(cancellationToken);

        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(table.Schema)}.{QuoteIdentifier(table.Name)};";
            var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            counts[table.FullName] = count;
        }

        logger.LogInformation("Quantidade de tabelas com dados encontrada: {Count}.", counts.Count);
        return counts;
    }

    private async Task<IReadOnlyList<TableRef>> ReadDumpTablesAsync(DatabaseSettings source, string dumpPath, CancellationToken cancellationToken)
    {
        var tables = new List<TableRef>();

        var startInfo = new ProcessStartInfo
        {
            FileName = "pg_restore",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("--list");
        startInfo.ArgumentList.Add(dumpPath);
        ConfigurePostgresEnvironment(startInfo, source);

        var result = await RunProcessAsync(
            startInfo,
            line =>
            {
                var match = TableDataRegex.Match(line);
                if (!match.Success)
                {
                    return;
                }

                var schema = UnquoteRestoreIdentifier(match.Groups[1].Value);
                var table = UnquoteRestoreIdentifier(match.Groups[2].Value);

                if (string.Equals(schema, "pg_catalog", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(schema, "information_schema", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                tables.Add(new TableRef(schema, table));
            },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Nao foi possivel ler a lista de tabelas do dump.");
        }

        return tables
            .DistinctBy(table => table.FullName)
            .ToArray();
    }

    private async Task RecreateTargetDatabaseAsync(DatabaseSettings target, CancellationToken cancellationToken)
    {
        logger.LogInformation("Recriando banco de destino '{Database}'.", target.Database);

        var builder = new NpgsqlConnectionStringBuilder(target.ToConnectionString())
        {
            Database = target.MaintenanceDatabase
        };

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText = """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName
                  AND pid <> pg_backend_pid();
                """;
            terminateCommand.Parameters.AddWithValue("databaseName", target.Database);
            await terminateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var dropCommand = connection.CreateCommand())
        {
            dropCommand.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(target.Database)};";
            await dropCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(target.Database)} WITH OWNER = {QuoteIdentifier(target.Username)};";
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("Banco de destino recriado com sucesso.");
    }

    private async Task RestoreSectionAsync(
        DatabaseSettings target,
        string dumpPath,
        string section,
        IReadOnlyDictionary<string, long> tableCounts,
        bool logDataStartsOnly,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Restaurando secao '{Section}'.", section);

        var currentTable = new StrongBox<string?>(null);
        var importedTables = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        var startInfo = new ProcessStartInfo
        {
            FileName = "pg_restore",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("--verbose");
        startInfo.ArgumentList.Add("--exit-on-error");
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("--no-privileges");
        startInfo.ArgumentList.Add($"--section={section}");
        startInfo.ArgumentList.Add($"--dbname={target.Database}");
        startInfo.ArgumentList.Add(dumpPath);
        ConfigurePostgresEnvironment(startInfo, target);

        var result = await RunProcessAsync(
            startInfo,
            line =>
            {
                if (TryLogDataProcessing(line, currentTable, importedTables))
                {
                    return;
                }

                if (!logDataStartsOnly)
                {
                    TryLogObjectCreation(line);
                }
            },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            var tableMessage = currentTable.Value is null
                ? string.Empty
                : $" Ultima tabela em processamento: {currentTable.Value}.";
            throw new InvalidOperationException($"A restauracao da secao '{section}' falhou.{tableMessage}");
        }

        if (logDataStartsOnly)
        {
            foreach (var table in importedTables.Keys.Order(StringComparer.Ordinal))
            {
                var count = tableCounts.GetValueOrDefault(table, 0);
                logger.LogInformation("Importacao concluida para {Table}: {Count} tuplas importadas.", table, count);
            }
        }

        logger.LogInformation("Secao '{Section}' restaurada com sucesso.", section);
    }

    private bool TryLogDataProcessing(string line, StrongBox<string?> currentTable, ConcurrentDictionary<string, byte> importedTables)
    {
        var match = DataProcessingRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        var tableName = NormalizeQualifiedName(match.Groups[1].Value);
        currentTable.Value = tableName;
        importedTables.TryAdd(tableName, 0);
        logger.LogInformation("Iniciando importacao da tabela {Table}.", tableName);
        return true;
    }

    private void TryLogObjectCreation(string line)
    {
        var match = CreationRegex.Match(line);
        if (!match.Success)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                logger.LogDebug("pg_restore: {Line}", line);
            }

            return;
        }

        logger.LogInformation("Restaurando objeto: {ObjectDescription}.", match.Groups[1].Value.Trim());
    }

    private void ConfigurePostgresEnvironment(ProcessStartInfo startInfo, DatabaseSettings settings)
    {
        startInfo.Environment["PGHOST"] = settings.Host;
        startInfo.Environment["PGPORT"] = settings.Port.ToString();
        startInfo.Environment["PGUSER"] = settings.Username;
        startInfo.Environment["PGPASSWORD"] = settings.Password;
        startInfo.Environment["PGDATABASE"] = settings.Database;
    }

    private async Task<ProcessResult> RunProcessAsync(
        ProcessStartInfo startInfo,
        Action<string> onOutputLine,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var outputLines = new ConcurrentQueue<string>();

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                return;
            }

            outputLines.Enqueue(eventArgs.Data);
            onOutputLine(eventArgs.Data);
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                return;
            }

            outputLines.Enqueue(eventArgs.Data);
            onOutputLine(eventArgs.Data);
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Nao foi possivel iniciar o processo '{startInfo.FileName}'.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(process.ExitCode, outputLines.ToArray());
    }

    private static string QuoteIdentifier(string value)
        => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string UnquoteRestoreIdentifier(string value)
        => value.Trim().Trim('"');

    private static string NormalizeQualifiedName(string value)
    {
        var cleaned = value.Trim();
        cleaned = cleaned.Replace("\".\"", ".", StringComparison.Ordinal);
        cleaned = cleaned.Replace("\"", string.Empty, StringComparison.Ordinal);
        return cleaned;
    }
}

internal sealed record TableRef(string Schema, string Name)
{
    public string FullName => $"{Schema}.{Name}";
}

internal sealed record ProcessResult(int ExitCode, IReadOnlyList<string> OutputLines);

internal sealed record DatabaseSettings(
    string Host,
    int Port,
    string Database,
    string Username,
    string Password,
    string MaintenanceDatabase);

internal sealed class CloneSettings
{
    public required DatabaseSettings Source { get; init; }
    public required DatabaseSettings Target { get; init; }

    public static CloneSettings LoadFromEnvironment()
    {
        return new CloneSettings
        {
            Source = LoadDatabaseSettings("SOURCE", fallbackToDefault: true),
            Target = LoadDatabaseSettings("TARGET", fallbackToDefault: false)
        };
    }

    private static DatabaseSettings LoadDatabaseSettings(string scope, bool fallbackToDefault)
    {
        var host = ReadRequired($"{scope}_POSTGRES_HOST", fallbackToDefault ? "POSTGRES_HOST" : null);
        var port = int.Parse(ReadRequired($"{scope}_POSTGRES_PORT", fallbackToDefault ? "POSTGRES_PORT" : null));
        var database = ReadRequired($"{scope}_POSTGRES_DB", fallbackToDefault ? "POSTGRES_DB" : null);
        var username = ReadRequired($"{scope}_POSTGRES_USER", fallbackToDefault ? "POSTGRES_USER" : null);
        var password = ReadRequired($"{scope}_POSTGRES_PASSWORD", fallbackToDefault ? "POSTGRES_PASSWORD" : null);
        var maintenanceDatabase = ReadOptional($"{scope}_POSTGRES_MAINTENANCE_DB")
            ?? ReadOptional("POSTGRES_MAINTENANCE_DB")
            ?? "postgres";

        return new DatabaseSettings(host, port, database, username, password, maintenanceDatabase);
    }

    private static string ReadRequired(string primaryName, string? fallbackName)
    {
        var value = ReadOptional(primaryName) ?? (fallbackName is null ? null : ReadOptional(fallbackName));
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Variavel de ambiente obrigatoria nao encontrada: {primaryName}.");
    }

    private static string? ReadOptional(string name)
        => Environment.GetEnvironmentVariable(name);
}

internal static class DatabaseSettingsExtensions
{
    public static string ToConnectionString(this DatabaseSettings settings)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password,
            Pooling = false
        }.ConnectionString;
    }
}
