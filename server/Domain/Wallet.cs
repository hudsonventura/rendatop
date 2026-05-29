using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Domain;

[Table("wallets")]
public class Wallet
{
    public const string DefaultName = "Carteira Principal";

    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey(nameof(owner_id))]
    [JsonIgnore]
    public User owner { get; set; } = null!;
    public Guid owner_id { get; set; }

    public string name { get; set; } = string.Empty;

    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
}
