using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Domain;

[Table("ai_usages")]
public class AiUsage
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    public Guid user_id { get; set; }

    [ForeignKey(nameof(user_id))]
    public User user { get; set; } = null!;

    public string feature { get; set; } = string.Empty;

    public string provider { get; set; } = string.Empty;

    public DateTime created_at { get; set; } = DateTime.UtcNow;
}
