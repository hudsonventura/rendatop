using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Domain;

/// <summary>
/// Status possíveis de uma assinatura
/// </summary>
public enum SubscriptionStatus
{
    Active,
    PendingPayment,
    Cancelled,
    Expired
}

/// <summary>
/// Representa a assinatura de um usuário a um plano.
/// </summary>
public class Subscription
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    /// <summary>
    /// Usuário dono desta assinatura
    /// </summary>
    [ForeignKey("user_id")]
    [JsonIgnore]
    public User user { get; set; } = null!;
    public Guid user_id { get; set; }

    /// <summary>
    /// Identificador do plano ("free", "plus", "pro")
    /// </summary>
    public string plan_id { get; set; } = "free";

    /// <summary>
    /// Status atual da assinatura
    /// </summary>
    public SubscriptionStatus status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// Método de pagamento usado ("credit_card", "debit_card", "boleto", "pix")
    /// </summary>
    public string payment_method { get; set; } = string.Empty;

    /// <summary>
    /// ID do último pagamento no Mercado Pago
    /// </summary>
    public string? mp_payment_id { get; set; }

    /// <summary>
    /// ID do customer no Mercado Pago (usado para cobranças recorrentes com cartão salvo)
    /// </summary>
    public string? mp_customer_id { get; set; }

    /// <summary>
    /// ID do cartão salvo no MP para renovação automática
    /// </summary>
    public string? mp_card_id { get; set; }

    /// <summary>
    /// Início do período de cobrança atual
    /// </summary>
    public DateTime current_period_start { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fim do período de cobrança atual
    /// </summary>
    public DateTime current_period_end { get; set; } = DateTime.UtcNow.AddMonths(1);

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime created_at { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Última atualização
    /// </summary>
    public DateTime updated_at { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Referência não-mapeada ao plano (preenchido manualmente via Plans.GetById)
    /// </summary>
    [NotMapped]
    public Plan? plan => Plans.GetById(plan_id);
}
