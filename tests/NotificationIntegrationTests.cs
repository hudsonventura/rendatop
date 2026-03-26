using System.Diagnostics;
using System.Net.Http;
using server.Utils;

namespace tests;

[Trait("Category", "Integration")]
public class NotificationIntegrationTests
{
    [Fact]
    public async Task EmailSmtp_SendsRealEmail_UsingDotEnvCredentials()
    {
        NotificationIntegrationEnvironment.Load();

        var smtp = new EmailSmtp(
            Environment.GetEnvironmentVariable("SMTP_HOST"),
            Environment.GetEnvironmentVariable("SMTP_PORT"),
            Environment.GetEnvironmentVariable("SMTP_USERNAME"),
            Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
            Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL"),
            Environment.GetEnvironmentVariable("SMTP_FROM_NAME"),
            Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL"));

        var destination = NotificationIntegrationEnvironment.GetRequired("SMTP_FROM_EMAIL");
        var marker = NotificationIntegrationEnvironment.BuildMarker("email");

        await smtp.Notify(
            destination,
            $"[Integration] Email {marker}",
            $"Teste de integração real de email. Marcador: {marker}");
    }

    [Fact]
    public async Task Telegram_SendsRealTelegramMessage_UsingDotEnvCredentials()
    {
        NotificationIntegrationEnvironment.Load();

        var telegram = new Telegram(
            NotificationIntegrationEnvironment.GetRequired("TELEGRAM_TOKEN"),
            NotificationIntegrationEnvironment.GetRequired("TELEGRAM_CHATID"));
        var marker = NotificationIntegrationEnvironment.BuildMarker("telegram");

        await telegram.Notify(
            "[Integration] Telegram",
            $"Teste de integração real do Telegram. Marcador: {marker}");
    }

    [Fact]
    public async Task WhatsApp_SendsRealMessage_UsingEvolutionContainerAndDotEnvCredentials()
    {
        NotificationIntegrationEnvironment.Load();
        await NotificationIntegrationEnvironment.EnsureEvolutionStackAsync();

        var whatsApp = new WhatsApp(
            NotificationIntegrationEnvironment.GetRequired("WHATSAPP_EVOLUTION_URL"),
            NotificationIntegrationEnvironment.GetRequired("WHATSAPP_EVOLUTION_INSTANCE"),
            NotificationIntegrationEnvironment.GetRequired("WHATSAPP_EVOLUTION_API_KEY"));
        var phone = NotificationIntegrationEnvironment.GetRequired("WHATSAPP_TEST_PHONE");
        var marker = NotificationIntegrationEnvironment.BuildMarker("whatsapp");

        await whatsApp.Notify(
            phone,
            "[Integration] WhatsApp",
            $"Teste de integração real do WhatsApp. Marcador: {marker}");
    }

    private static class NotificationIntegrationEnvironment
    {
        private static bool _loaded;

        public static void Load()
        {
            if (_loaded)
                return;

            var envPath = Path.Combine(GetRepositoryRoot(), ".env");
            if (!File.Exists(envPath))
                throw new InvalidOperationException($".env não encontrado em {envPath}");

            foreach (var rawLine in File.ReadAllLines(envPath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, value);
            }

            _loaded = true;
        }

        public static string GetRequired(string key)
        {
            var value = Environment.GetEnvironmentVariable(key)?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Variável de ambiente obrigatória ausente para teste de integração: {key}");

            return value;
        }

        public static string BuildMarker(string channel) =>
            $"{channel}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        public static async Task EnsureEvolutionStackAsync()
        {
            var repositoryRoot = GetRepositoryRoot();
            await RunProcessAsync(
                "docker",
                "compose up -d db redis evolution",
                repositoryRoot);

            var baseUrl = GetRequired("WHATSAPP_EVOLUTION_URL").TrimEnd('/');
            await WaitForHttpAsync($"{baseUrl}/");
        }

        private static async Task WaitForHttpAsync(string url)
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            Exception? lastError = null;
            for (var attempt = 0; attempt < 24; attempt++)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if ((int)response.StatusCode < 500)
                        return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            throw new InvalidOperationException(
                $"Evolution API não respondeu em tempo hábil em {url}.",
                lastError);
        }

        private static async Task RunProcessAsync(string fileName, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Não foi possível iniciar o processo: {fileName} {arguments}");

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Comando falhou: {fileName} {arguments}{Environment.NewLine}{stdOut}{Environment.NewLine}{stdErr}");
            }
        }

        private static string GetRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "docker-compose.yml")))
                    return current.FullName;

                current = current.Parent;
            }

            throw new InvalidOperationException("Não foi possível localizar a raiz do repositório para os testes de integração.");
        }
    }
}
