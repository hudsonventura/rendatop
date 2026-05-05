using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using server.Utils;

namespace tests;

[Trait("Category", "Integration")]
public class NotificationIntegrationTests
{

    ILogger<server.Utils.Telegram> _logger = new LoggerFactory().CreateLogger<server.Utils.Telegram>();
    static ILogger<server.Utils.Telegram> logger = new LoggerFactory().CreateLogger<server.Utils.Telegram>();

    [Fact]
    public async Task EmailSmtp_SendsRealEmail_UsingDotEnvCredentials()
    {
        NotificationIntegrationEnvironment.Load();

        var smtp = NotificationIntegrationEnvironment.CreateSmtp();

        var destination = NotificationIntegrationEnvironment.GetRequired("SMTP_FROM_EMAIL");
        var marker = NotificationIntegrationEnvironment.BuildMarker("email");

        await smtp.Notify(
            destination,
            $"[Integration] Email {marker}",
            $"Teste de integração real de email. Marcador: {marker}");
    }

    [Fact]
    public async Task EmailSmtp_SendsRealSubscriptionReceiptHtml_ToConfiguredTestInbox()
    {
        NotificationIntegrationEnvironment.Load();

        var smtp = NotificationIntegrationEnvironment.CreateSmtp();
        var destination = NotificationIntegrationEnvironment.GetRequired("SMTP_EMAIL_TESTS");
        var marker = NotificationIntegrationEnvironment.BuildMarker("receipt");

        var user = new server.Domain.User
        {
            name = "Hudson Ventura",
            email = destination,
            password = "integration-test"
        };

        var charge = new server.Domain.SubscriptionCharge
        {
            plan_id = "plus",
            payment_method = "credit_card",
            payer_cpf = "12345678909",
            amount = 6.90m,
            charge_kind = server.Domain.SubscriptionChargeKind.Initial,
            billing_period_start = DateTime.UtcNow,
            billing_period_end = DateTime.UtcNow.AddMonths(1),
            due_at = DateTime.UtcNow.AddMonths(1),
            approved_at = DateTime.UtcNow
        };

        var html = SubscriptionReceiptEmailTemplate.Build(
            user,
            charge,
            Environment.GetEnvironmentVariable("BASE_URL_CLIENT"));

        await smtp.Notify(
            destination,
            $"[Integration] Recibo de assinatura {marker}",
            html,
            isHtml: true);
    }

    [Fact]
    public async Task EmailSmtp_SendsRealSubscriptionCancellationHtml_ToConfiguredTestInbox()
    {
        NotificationIntegrationEnvironment.Load();

        var smtp = NotificationIntegrationEnvironment.CreateSmtp();
        var destination = NotificationIntegrationEnvironment.GetRequired("SMTP_EMAIL_TESTS");
        var marker = NotificationIntegrationEnvironment.BuildMarker("cancellation");

        var user = new server.Domain.User
        {
            name = "Hudson Ventura",
            email = destination,
            password = "integration-test"
        };

        var subscription = new server.Domain.Subscription
        {
            plan_id = "plus",
            payment_method = "credit_card",
            current_period_start = DateTime.UtcNow.AddDays(-12),
            current_period_end = DateTime.UtcNow.AddDays(18),
            cancellation_requested_at = DateTime.UtcNow
        };

        var result = new server.Services.SubscriptionCancellationResult
        {
            cancelled = true,
            scheduled = false,
            refunded_amount = 4.35m,
            effective_at = DateTime.UtcNow,
            message = "Assinatura cancelada imediatamente. Reembolso proporcional solicitado: R$ 4,35."
        };

        var html = SubscriptionCancellationEmailTemplate.Build(
            user,
            subscription,
            result,
            Environment.GetEnvironmentVariable("BASE_URL_CLIENT"));

        await smtp.Notify(
            destination,
            $"[Integration] Cancelamento de assinatura {marker}",
            html,
            isHtml: true);
    }

    [Fact]
    public async Task Telegram_SendsRealTelegramMessage_UsingDotEnvCredentials()
    {
        NotificationIntegrationEnvironment.Load();


        var telegram = new server.Utils.Telegram(
            _logger,
            NotificationIntegrationEnvironment.GetRequired("TELEGRAM_TOKEN"),
            NotificationIntegrationEnvironment.GetRequired("TELEGRAM_CHATID"));
        var marker = NotificationIntegrationEnvironment.BuildMarker("telegram");

        await telegram.Notify(
            "[Integration] Telegram",
            $"Teste de integração real do Telegram. Marcador: {marker}");
    }

    [Fact]
    public async Task WhatsApp_SendsRealMessage_UsingConfiguredProviderAndDotEnvCredentials()
    {
        NotificationIntegrationEnvironment.Load();
        await NotificationIntegrationEnvironment.EnsureWhatsAppStackAsync();

        var whatsApp = NotificationIntegrationEnvironment.CreateWhatsAppNotification();
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

        public static EmailSmtp CreateSmtp()
        {
            ILogger<EmailSmtp> logger = new LoggerFactory().CreateLogger<EmailSmtp>();

            return new EmailSmtp(
                logger,
                Environment.GetEnvironmentVariable("SMTP_HOST"),
                Environment.GetEnvironmentVariable("SMTP_PORT"),
                Environment.GetEnvironmentVariable("SMTP_USERNAME"),
                Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
                Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL"),
                Environment.GetEnvironmentVariable("SMTP_FROM_NAME"),
                Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL"));
        }

        public static IWhatsAppNotification CreateWhatsAppNotification()
        {
            ILogger<WWebJsWhatsAppNotification> logger1 = new LoggerFactory().CreateLogger<WWebJsWhatsAppNotification>();
            ILogger<WhatsApp> logger2 = new LoggerFactory().CreateLogger<WhatsApp>();

            var provider = (Environment.GetEnvironmentVariable("WHATSAPP_PROVIDER") ?? "evolution").Trim().ToLowerInvariant();
            return provider switch
            {
                "wwebjs" => new WWebJsWhatsAppNotification(
                    logger1,
                    GetRequired("WHATSAPP_WWEBJS_URL"),
                    Environment.GetEnvironmentVariable("WHATSAPP_WWEBJS_API_KEY"),
                    Environment.GetEnvironmentVariable("WHATSAPP_WWEBJS_SESSION_ID")),
                _ => new WhatsApp(
                    logger2,
                    GetRequired("WHATSAPP_EVOLUTION_URL"),
                    GetRequired("WHATSAPP_EVOLUTION_INSTANCE"),
                    GetRequired("WHATSAPP_EVOLUTION_API_KEY"))
            };
        }

        public static async Task EnsureWhatsAppStackAsync()
        {
            var provider = (Environment.GetEnvironmentVariable("WHATSAPP_PROVIDER") ?? "evolution").Trim().ToLowerInvariant();
            if (provider == "wwebjs")
            {
                var baseUrl = GetRequired("WHATSAPP_WWEBJS_URL").TrimEnd('/');
                await WaitForHttpAsync($"{baseUrl}/ping");
                return;
            }

            await EnsureEvolutionStackAsync();
        }

        private static async Task EnsureEvolutionStackAsync()
        {
            var baseUrl = GetRequired("WHATSAPP_EVOLUTION_URL").TrimEnd('/');
            if (!IsManagedByCompose())
            {
                var repositoryRoot = GetRepositoryRoot();
                await RunProcessAsync(
                    "docker",
                    "compose up -d db redis evolution",
                    repositoryRoot);
            }

            await WaitForHttpAsync($"{baseUrl}/");
        }

        private static bool IsManagedByCompose()
        {
            var managedByCompose = Environment.GetEnvironmentVariable("NOTIFICATION_TESTS_MANAGED_BY_COMPOSE");
            if (string.Equals(managedByCompose, "true", StringComparison.OrdinalIgnoreCase))
                return true;

            var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            return string.Equals(runningInContainer, "true", StringComparison.OrdinalIgnoreCase);
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
                $"Serviço HTTP não respondeu em tempo hábil em {url}.",
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
