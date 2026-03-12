using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Domain;

[Table("notifications")]
public class Notification
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("user")]
    public Guid user_id { get; set; }
    public User user { get; set; }

    public string title { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public string? source_key { get; set; }

    public bool is_read { get; set; } = false;

    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime? read_at { get; set; }
}
