using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using server.Utils;

namespace server.BackgroundServices;

public class TelegramBotBackgroundService : BackgroundService
{
    private readonly ILogger<TelegramBotBackgroundService> _logger;
    private readonly string? _token;
    private TelegramBotClient? _botClient;

    private string _TraceId = string.Empty;
    private readonly List<string> _tags = new() { "TelegramBot", "BackgroundService" };

    public TelegramBotBackgroundService(ILogger<TelegramBotBackgroundService> logger)
    {
        _logger = logger;
        _token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            _logger.LogWarning("Serviço do bot do Telegram não iniciado porque TELEGRAM_TOKEN não está configurado. {_tags_}", _tags);
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

        _logger.LogInformation("Serviço do bot do Telegram iniciado com StartReceiving. {TraceId} {_tags_}", _TraceId, _tags);

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

        _logger.LogInformation("Serviço do bot do Telegram finalizado. {TraceId} {_tags_}", _TraceId, _tags);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        using var activity = TraceContext.StartActivity("background.telegram-bot-update");
        var traceId = TraceContext.GetTraceId();
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

        _logger.LogInformation("Chat ID enviado via /start. TraceId={TraceId} ChatId={ChatId} {_tags_}", traceId, chatId, _tags);
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = TraceContext.GetTraceId();
        if (exception is ApiRequestException apiException)
        {
            _logger.LogError(
                exception,
                "Erro da API do Telegram no StartReceiving. TraceId={TraceId} Code={Code} Message={Message} {_tags_}",
                traceId,
                apiException.ErrorCode,
                apiException.Message,
                _tags);
        }
        else
        {
            _logger.LogError(exception, "Erro no polling do bot do Telegram. TraceId={TraceId} {_tags_}", traceId, _tags);
        }

        return Task.CompletedTask;
    }
}
