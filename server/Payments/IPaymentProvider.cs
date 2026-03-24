namespace server.Payments;

/// <summary>
/// Interface genérica para provedores de pagamento.
/// Permite substituir o Mercado Pago por outro provedor (Stripe, PagSeguro, etc.)
/// sem alterar controllers ou lógica de negócio.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Cria um pagamento com cartão de crédito/débito usando token gerado no frontend
    /// </summary>
    Task<PaymentResult> CreateCardPaymentAsync(CardPaymentRequest request);

    /// <summary>
    /// Cria um pagamento via PIX, retornando QR Code e código copia-e-cola
    /// </summary>
    Task<PaymentResult> CreatePixPaymentAsync(PixPaymentRequest request);

    /// <summary>
    /// Cria um pagamento via boleto bancário, retornando código de barras e URL
    /// </summary>
    Task<PaymentResult> CreateBoletoPaymentAsync(BoletoPaymentRequest request);

    /// <summary>
    /// Consulta o status de um pagamento pelo ID do provedor
    /// </summary>
    Task<PaymentResult> GetPaymentStatusAsync(string paymentId);

    /// <summary>
    /// Salva o cartão do cliente no provedor para cobranças futuras.
    /// Retorna customer_id e card_id.
    /// </summary>
    Task<(string customerId, string cardId)> SaveCardAsync(string cardToken, string email);

    /// <summary>
    /// Cria um pagamento usando um cartão previamente salvo (renovação automática)
    /// </summary>
    Task<PaymentResult> CreateSavedCardPaymentAsync(SavedCardPaymentRequest request);
}
