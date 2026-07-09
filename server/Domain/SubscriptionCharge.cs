using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Domain;

public enum SubscriptionChargeStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Expired
}

public enum SubscriptionChargeKind
{
    Initial,
    Renewal
}

[Table("subscription_charges")]
public class SubscriptionCharge
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("subscription_id")]
    [JsonIgnore]
    public Subscription subscription { get; set; } = null!;
    public Guid subscription_id { get; set; }

    [ForeignKey("user_id")]
    [JsonIgnore]
    public User user { get; set; } = null!;
    public Guid user_id { get; set; }

    public string plan_id { get; set; } = string.Empty;
    public string payment_method { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string payer_cpf { get; set; } = string.Empty;

    public string? provider_payment_id { get; set; }
    public string? provider_subscription_id { get; set; }
    public string? provider_external_reference { get; set; }
    public string? provider_checkout_url { get; set; }
    public string? provider_status_detail { get; set; }

    public SubscriptionChargeStatus status { get; set; } = SubscriptionChargeStatus.Pending;
    public SubscriptionChargeKind charge_kind { get; set; } = SubscriptionChargeKind.Initial;

    public DateTime billing_period_start { get; set; } = DateTime.UtcNow;
    public DateTime billing_period_end { get; set; } = DateTime.UtcNow.AddMonths(1);
    public DateTime? due_at { get; set; }
    public DateTime? approved_at { get; set; }
    public DateTime? reminder_sent_at { get; set; }
    public DateTime? receipt_sent_at { get; set; }

    public string? pix_qr_code { get; set; }
    public string? pix_qr_code_base64 { get; set; }
    public string? boleto_barcode_content { get; set; }
    public string? boleto_barcode_image_base64 { get; set; }
    public string? boleto_digitable_line { get; set; }
    public string? boleto_url { get; set; }

    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
}
