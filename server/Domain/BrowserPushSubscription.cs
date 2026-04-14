using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Domain;

[Table("browser_push_subscriptions")]
public class BrowserPushSubscription
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    public Guid user_id { get; set; }

    [ForeignKey(nameof(user_id))]
    public User? user { get; set; }

    public string endpoint { get; set; } = string.Empty;

    public string p256dh { get; set; } = string.Empty;

    public string auth { get; set; } = string.Empty;

    public string? user_agent { get; set; }

    public DateTime created_at { get; set; } = DateTime.UtcNow;

    public DateTime updated_at { get; set; } = DateTime.UtcNow;
}
