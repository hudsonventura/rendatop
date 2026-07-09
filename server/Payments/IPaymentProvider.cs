namespace server.Payments;

/// <summary>
/// Interface genérica para provedores de pagamento.
/// Permite substituir o Mercado Pago por outro provedor (Stripe, PagSeguro, etc.)
/// sem alterar controllers ou lógica de negócio.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Cria uma assinatura com checkout hospedado e retorna a URL para redirecionamento.
    /// </summary>
    Task<PaymentResult> CreateHostedSubscriptionAsync(HostedSubscriptionRequest request, CancellationToken cancellationToken = default);

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
    /// Consulta o status de uma assinatura/preapproval pelo ID do provedor.
    /// </summary>
    Task<PaymentResult> GetSubscriptionStatusAsync(string preapprovalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta um pagamento autorizado/recorrente originado de uma assinatura.
    /// </summary>
    Task<PaymentResult> GetAuthorizedPaymentStatusAsync(string authorizedPaymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela uma assinatura hospedada no provedor para interromper cobranças futuras.
    /// </summary>
    Task CancelSubscriptionAsync(string preapprovalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Salva o cartão do cliente no provedor para cobranças futuras.
    /// Retorna customer_id e card_id.
    /// </summary>
    Task<(string customerId, string cardId)> SaveCardAsync(string cardToken, string email);

    /// <summary>
    /// Cria um pagamento usando um cartão previamente salvo (renovação automática)
    /// </summary>
    Task<PaymentResult> CreateSavedCardPaymentAsync(SavedCardPaymentRequest request);

    /// <summary>
    /// Solicita estorno total ou parcial de um pagamento.
    /// </summary>
    Task<PaymentRefundResult> RefundPaymentAsync(string paymentId, decimal? amount = null, CancellationToken cancellationToken = default);
}
