using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Domain;

[Table("landing_visits")]
public class LandingVisit
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    public string visit { get; set; } = "direct";

    public string ip_address { get; set; } = string.Empty;

    public string? user_agent { get; set; }

    public string? referrer { get; set; }

    public DateTime created_at { get; set; } = DateTime.UtcNow;
}
