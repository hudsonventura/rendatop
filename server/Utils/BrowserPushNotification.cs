using System.Text.Json;
using Lib.Net.Http.WebPush;
using server.Domain;

namespace server.Utils;

public class BrowserPushNotification : IBrowserPushNotification
{
    private readonly ILogger<BrowserPushNotification> _logger;
    private readonly PushServiceClient _pushServiceClient;

    private readonly static List<string> _tags = new() { "BrowserPushNotification", "Notification" };

    public bool IsConfigured { get; }
    public string PublicKey { get; }

    public BrowserPushNotification(
        ILogger<BrowserPushNotification> logger,
        PushServiceClient pushServiceClient,
        string? publicKey,
        string? privateKey)
    {
        _logger = logger;
        _pushServiceClient = pushServiceClient;
        PublicKey = (publicKey ?? string.Empty).Trim();
        var normalizedPrivateKey = (privateKey ?? string.Empty).Trim();
        IsConfigured = !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(normalizedPrivateKey);
    }

    public async Task SendAsync(BrowserPushSubscription subscription, BrowserPushMessage message, CancellationToken cancellationToken = default)
    {
        var traceId = TraceContext.GetTraceId();
        if (!IsConfigured)
            throw new Exception("Web Push não está configurado no servidor.");

        var pushSubscription = new PushSubscription
        {
            Endpoint = subscription.endpoint,
            Keys = new Dictionary<string, string>
            {
                ["p256dh"] = subscription.p256dh,
                ["auth"] = subscription.auth
            }
        };

        var payload = JsonSerializer.Serialize(new
        {
            title = message.Title,
            body = message.Body,
            url = message.Url,
            tag = message.Tag
        });

        var pushMessage = new PushMessage(payload)
        {
            TimeToLive = 60 * 60 * 12
        };

        try
        {
            await _pushServiceClient.RequestPushMessageDeliveryAsync(pushSubscription, pushMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao enviar Browser Push. TraceId={TraceId} Payload={@Payload} Tags={_tags_}",
                traceId,
                new
                {
                    subscription.user_id,
                    subscription.endpoint,
                    message.Title,
                    message.Tag,
                    message.Url
                },
                _tags);
            throw;
        }
    }
}
