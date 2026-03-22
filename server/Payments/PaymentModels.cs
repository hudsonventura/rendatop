namespace server.Payments;

/// <summary>
/// Resultado de um pagamento processado
/// </summary>
public class PaymentResult
{
    /// <summary>
    /// Status do pagamento retornado pelo provedor ("approved", "pending", "rejected", etc.)
    /// </summary>
    public string status { get; set; } = string.Empty;

    /// <summary>
    /// Detalhe do status (ex: "accredited", "pending_waiting_transfer")
    /// </summary>
    public string status_detail { get; set; } = string.Empty;

    /// <summary>
    /// ID do pagamento no provedor
    /// </summary>
    public string payment_id { get; set; } = string.Empty;

    // --- PIX ---

    /// <summary>
    /// Código PIX copia-e-cola
    /// </summary>
    public string? pix_qr_code { get; set; }

    /// <summary>
    /// QR Code em base64 (imagem PNG)
    /// </summary>
    public string? pix_qr_code_base64 { get; set; }

    // --- Boleto ---

    /// <summary>
    /// Código de barras numérico do boleto.
    /// </summary>
    public string? boleto_barcode_content { get; set; }

    /// <summary>
    /// Imagem PNG do código de barras em base64.
    /// </summary>
    public string? boleto_barcode_image_base64 { get; set; }

    /// <summary>
    /// Linha digitável do boleto.
    /// </summary>
    public string? boleto_digitable_line { get; set; }

    /// <summary>
    /// URL do boleto para download/impressão
    /// </summary>
    public string? boleto_url { get; set; }

    // --- Cartão ---

    /// <summary>
    /// ID do customer no provedor (para cobranças futuras)
    /// </summary>
    public string? customer_id { get; set; }

    /// <summary>
    /// ID do cartão salvo no provedor (para renovação automática)
    /// </summary>
    public string? card_id { get; set; }
}

/// <summary>
/// Dados para pagamento com cartão
/// </summary>
public class CardPaymentRequest
{
    public string card_token { get; set; } = string.Empty;
    public string payment_method_id { get; set; } = string.Empty;
    public string card_type { get; set; } = "credit_card";
    public string issuer_id { get; set; } = string.Empty;
    public int installments { get; set; } = 1;
    public decimal amount { get; set; }
    public string description { get; set; } = string.Empty;
    public string payer_email { get; set; } = string.Empty;
    public string external_reference { get; set; } = string.Empty;
}

/// <summary>
/// Dados para pagamento via PIX
/// </summary>
public class PixPaymentRequest
{
    public decimal amount { get; set; }
    public string description { get; set; } = string.Empty;
    public string payer_email { get; set; } = string.Empty;
    public string payer_first_name { get; set; } = string.Empty;
    public string payer_last_name { get; set; } = string.Empty;
    public string payer_cpf { get; set; } = string.Empty;
    public string external_reference { get; set; } = string.Empty;
}

/// <summary>
/// Dados para pagamento via boleto
/// </summary>
public class BoletoPaymentRequest
{
    public decimal amount { get; set; }
    public string description { get; set; } = string.Empty;
    public string payer_email { get; set; } = string.Empty;
    public string payer_first_name { get; set; } = string.Empty;
    public string payer_last_name { get; set; } = string.Empty;
    public string payer_cpf { get; set; } = string.Empty;
    public string external_reference { get; set; } = string.Empty;
}

/// <summary>
/// Dados para renovação de pagamento com cartão salvo
/// </summary>
public class SavedCardPaymentRequest
{
    public string customer_id { get; set; } = string.Empty;
    public string card_id { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string description { get; set; } = string.Empty;
    public string external_reference { get; set; } = string.Empty;
}
