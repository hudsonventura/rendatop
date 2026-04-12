using System.Text.Json;
using Lib.Net.Http.WebPush;
using server.Domain;

namespace server.Utils;

public class BrowserPushNotification : IBrowserPushNotification
{
    private readonly PushServiceClient _pushServiceClient;

    public bool IsConfigured { get; }
    public string PublicKey { get; }

    public BrowserPushNotification(
        PushServiceClient pushServiceClient,
        string? publicKey,
        string? privateKey)
    {
        _pushServiceClient = pushServiceClient;
        PublicKey = (publicKey ?? string.Empty).Trim();
        var normalizedPrivateKey = (privateKey ?? string.Empty).Trim();
        IsConfigured = !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(normalizedPrivateKey);
    }

    public async Task SendAsync(BrowserPushSubscription subscription, BrowserPushMessage message, CancellationToken cancellationToken = default)
    {
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

        await _pushServiceClient.RequestPushMessageDeliveryAsync(pushSubscription, pushMessage, cancellationToken);
    }
}
