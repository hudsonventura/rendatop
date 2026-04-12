using server.Domain;

namespace server.Utils;

public interface IBrowserPushNotification
{
    bool IsConfigured { get; }
    string PublicKey { get; }
    Task SendAsync(BrowserPushSubscription subscription, BrowserPushMessage message, CancellationToken cancellationToken = default);
}

public sealed record BrowserPushMessage(
    string Title,
    string Body,
    string? Url = null,
    string? Tag = null
);
