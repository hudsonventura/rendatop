using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record PlanDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    [JsonPropertyName("features")]
    public Dictionary<string, string> Features { get; init; } = [];
}

public sealed record SubscriptionOverviewDto
{
    [JsonPropertyName("active_subscription")]
    public SubscriptionDto? ActiveSubscription { get; init; }

    [JsonPropertyName("pending_subscription")]
    public SubscriptionDto? PendingSubscription { get; init; }

    [JsonPropertyName("pending_charge")]
    public SubscriptionChargeDto? PendingCharge { get; init; }
}

public sealed record SubscriptionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("plan_id")]
    public string PlanId { get; init; } = "free";

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; init; } = string.Empty;

    [JsonPropertyName("current_period_end")]
    public DateTime CurrentPeriodEnd { get; init; }

    [JsonPropertyName("cancel_at_period_end")]
    public bool CancelAtPeriodEnd { get; init; }

    [JsonPropertyName("cancellation_requested_at")]
    public DateTime? CancellationRequestedAt { get; init; }

    [JsonPropertyName("plan")]
    public PlanDto? Plan { get; init; }
}

public sealed record SubscriptionChargeDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("plan_id")]
    public string PlanId { get; init; } = string.Empty;

    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("provider_payment_id")]
    public string? ProviderPaymentId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("charge_kind")]
    public string ChargeKind { get; init; } = string.Empty;

    [JsonPropertyName("due_at")]
    public DateTime? DueAt { get; init; }

    [JsonPropertyName("pix_qr_code")]
    public string? PixQrCode { get; init; }

    [JsonPropertyName("pix_qr_code_base64")]
    public string? PixQrCodeBase64 { get; init; }

    [JsonPropertyName("boleto_barcode_content")]
    public string? BoletoBarcodeContent { get; init; }

    [JsonPropertyName("boleto_barcode_image_base64")]
    public string? BoletoBarcodeImageBase64 { get; init; }

    [JsonPropertyName("boleto_digitable_line")]
    public string? BoletoDigitableLine { get; init; }

    [JsonPropertyName("boleto_url")]
    public string? BoletoUrl { get; init; }
}

public sealed record PaymentResultDto
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("status_detail")]
    public string StatusDetail { get; init; } = string.Empty;

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; init; } = string.Empty;

    [JsonPropertyName("checkout_url")]
    public string? CheckoutUrl { get; init; }

    [JsonPropertyName("preference_id")]
    public string? PreferenceId { get; init; }

    [JsonPropertyName("preapproval_id")]
    public string? PreapprovalId { get; init; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; init; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; init; }

    [JsonPropertyName("approved_at")]
    public DateTime? ApprovedAt { get; init; }

    [JsonPropertyName("date_of_expiration")]
    public DateTime? DateOfExpiration { get; init; }

    [JsonPropertyName("pix_qr_code")]
    public string? PixQrCode { get; init; }

    [JsonPropertyName("pix_qr_code_base64")]
    public string? PixQrCodeBase64 { get; init; }

    [JsonPropertyName("boleto_barcode_content")]
    public string? BoletoBarcodeContent { get; init; }

    [JsonPropertyName("boleto_barcode_image_base64")]
    public string? BoletoBarcodeImageBase64 { get; init; }

    [JsonPropertyName("boleto_digitable_line")]
    public string? BoletoDigitableLine { get; init; }

    [JsonPropertyName("boleto_url")]
    public string? BoletoUrl { get; init; }

    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("card_id")]
    public string? CardId { get; init; }
}

public sealed record CardHostedCheckoutRequestDto(
    [property: JsonPropertyName("plan_id")] string PlanId,
    [property: JsonPropertyName("payer_cpf")] string PayerCpf);

public sealed record PixHostedCheckoutRequestDto(
    [property: JsonPropertyName("plan_id")] string PlanId,
    [property: JsonPropertyName("payer_first_name")] string PayerFirstName,
    [property: JsonPropertyName("payer_last_name")] string PayerLastName,
    [property: JsonPropertyName("payer_cpf")] string PayerCpf);

public sealed record BoletoHostedCheckoutRequestDto(
    [property: JsonPropertyName("plan_id")] string PlanId,
    [property: JsonPropertyName("payer_first_name")] string PayerFirstName,
    [property: JsonPropertyName("payer_last_name")] string PayerLastName,
    [property: JsonPropertyName("payer_cpf")] string PayerCpf);

public sealed record CancelSubscriptionRequestDto(
    [property: JsonPropertyName("confirm")] bool Confirm,
    [property: JsonPropertyName("mode")] string? Mode);

public sealed record RevertScheduledCancellationRequestDto(
    [property: JsonPropertyName("confirm")] bool Confirm);

public sealed record CancelSubscriptionResultDto
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
