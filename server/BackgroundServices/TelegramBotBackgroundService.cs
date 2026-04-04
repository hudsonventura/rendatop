using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace server.BackgroundServices;

public class TelegramBotBackgroundService : BackgroundService
{
    private readonly ILogger<TelegramBotBackgroundService> _logger;
    private readonly string? _token;
    private TelegramBotClient? _botClient;

    public TelegramBotBackgroundService(ILogger<TelegramBotBackgroundService> logger)
    {
        _logger = logger;
        _token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            _logger.LogWarning("Serviço do bot do Telegram não iniciado porque TELEGRAM_TOKEN não está configurado.");
            return;
        }

        _botClient = new TelegramBotClient(_token);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Serviço do bot do Telegram iniciado com StartReceiving.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Serviço do bot do Telegram finalizado.");
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message is null)
            return;

        var message = update.Message;
        if (message.Type != MessageType.Text || string.IsNullOrWhiteSpace(message.Text))
            return;

        var text = message.Text.Trim();
        if (!text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            return;

        var chatId = message.Chat.Id.ToString();
        var response =
            "Olá! Este é o seu chatID do Telegram para usar no RendaTop:" + Environment.NewLine +
            Environment.NewLine +
            $"`{chatId}`" + Environment.NewLine +
            Environment.NewLine +
            "Copie esse número e cole no campo Chat ID do Telegram nas suas configurações.";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: response,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Chat ID enviado via /start para chat={ChatId}.", chatId);
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ApiRequestException apiException)
        {
            _logger.LogError(
                exception,
                "Erro da API do Telegram no StartReceiving. Code={Code} Message={Message}",
                apiException.ErrorCode,
                apiException.Message);
        }
        else
        {
            _logger.LogError(exception, "Erro no polling do bot do Telegram.");
        }

        return Task.CompletedTask;
    }
}
