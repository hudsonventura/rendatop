using MercadoPago.Client;
using MercadoPago.Client.Customer;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Customer;
using MercadoPago.Resource.Payment;

namespace server.Payments.MercadoPago;

/// <summary>
/// Implementação do IPaymentProvider usando a API do Mercado Pago.
/// Usa a Payments API (/v1/payments) para transparência total no checkout.
/// </summary>
public class MercadoPagoPaymentProvider : IPaymentProvider
{
    private readonly ILogger<MercadoPagoPaymentProvider> _logger;

    public MercadoPagoPaymentProvider(ILogger<MercadoPagoPaymentProvider> logger)
    {
        _logger = logger;
        string? accessToken = Environment.GetEnvironmentVariable("MERCADO_PAGO_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("MERCADO_PAGO_ACCESS_TOKEN não configurado.");

        // Log para debug — remover em produção
        var trimmed = accessToken.Trim();
        _logger.LogInformation(
            "MP AccessToken carregado: length={Len}, starts={Start}..., ends=...{End}",
            trimmed.Length,
            trimmed.Length > 15 ? trimmed[..15] : trimmed,
            trimmed.Length > 10 ? trimmed[^10..] : trimmed
        );

        MercadoPagoConfig.AccessToken = trimmed;
    }


    public async Task<PaymentResult> CreateCardPaymentAsync(CardPaymentRequest request)
    {
        var client = new PaymentClient();

        var paymentRequest = new PaymentCreateRequest
        {
            TransactionAmount = request.amount,
            Token = request.card_token,
            Description = request.description,
            Installments = request.installments,
            PaymentMethodId = request.payment_method_id,
            ExternalReference = request.external_reference,
            Payer = new PaymentPayerRequest
            {
                Email = request.payer_email
            }
        };

        if (!string.IsNullOrEmpty(request.issuer_id))
            paymentRequest.IssuerId = request.issuer_id;

        var payment = await client.CreateAsync(paymentRequest);
        _logger.LogInformation("Pagamento cartão criado: {Id} status={Status}", payment.Id, payment.Status);

        return new PaymentResult
        {
            payment_id = payment.Id?.ToString() ?? "",
            status = payment.Status ?? "unknown",
            status_detail = payment.StatusDetail ?? ""
        };
    }


    public async Task<PaymentResult> CreatePixPaymentAsync(PixPaymentRequest request)
    {
        var client = new PaymentClient();

        var paymentRequest = new PaymentCreateRequest
        {
            TransactionAmount = request.amount,
            Description = request.description,
            PaymentMethodId = "pix",
            ExternalReference = request.external_reference,
            Payer = new PaymentPayerRequest
            {
                Email = request.payer_email,
                FirstName = request.payer_first_name,
                LastName = request.payer_last_name,
                Identification = new global::MercadoPago.Client.Common.IdentificationRequest
                {
                    Type = "CPF",
                    Number = request.payer_cpf
                }
            }
        };

        var payment = await client.CreateAsync(paymentRequest);
        _logger.LogInformation("Pagamento PIX criado: {Id} status={Status}", payment.Id, payment.Status);

        var pointOfInteraction = payment.PointOfInteraction;
        var transactionData = pointOfInteraction?.TransactionData;

        return new PaymentResult
        {
            payment_id = payment.Id?.ToString() ?? "",
            status = payment.Status ?? "unknown",
            status_detail = payment.StatusDetail ?? "",
            pix_qr_code = transactionData?.QrCode,
            pix_qr_code_base64 = transactionData?.QrCodeBase64
        };
    }


    public async Task<PaymentResult> CreateBoletoPaymentAsync(BoletoPaymentRequest request)
    {
        var client = new PaymentClient();

        var paymentRequest = new PaymentCreateRequest
        {
            TransactionAmount = request.amount,
            Description = request.description,
            PaymentMethodId = "bolbradesco",
            ExternalReference = request.external_reference,
            Payer = new PaymentPayerRequest
            {
                Email = request.payer_email,
                FirstName = request.payer_first_name,
                LastName = request.payer_last_name,
                Identification = new global::MercadoPago.Client.Common.IdentificationRequest
                {
                    Type = "CPF",
                    Number = request.payer_cpf
                }
            }
        };

        var payment = await client.CreateAsync(paymentRequest);
        _logger.LogInformation("Pagamento boleto criado: {Id} status={Status}", payment.Id, payment.Status);

        // barcode_content via TransactionDetails (Barcode property not available in all SDK versions)
        string? barcodeContent = null;
        try { barcodeContent = payment.TransactionDetails?.DigitableLine; } catch { }

        return new PaymentResult
        {
            payment_id = payment.Id?.ToString() ?? "",
            status = payment.Status ?? "unknown",
            status_detail = payment.StatusDetail ?? "",
            boleto_barcode_content = barcodeContent,
            boleto_url = payment.TransactionDetails?.ExternalResourceUrl
        };
    }


    public async Task<PaymentResult> GetPaymentStatusAsync(string paymentId)
    {
        var client = new PaymentClient();
        var payment = await client.GetAsync(long.Parse(paymentId));

        return new PaymentResult
        {
            payment_id = payment.Id?.ToString() ?? "",
            status = payment.Status ?? "unknown",
            status_detail = payment.StatusDetail ?? ""
        };
    }


    public async Task<(string customerId, string cardId)> SaveCardAsync(string cardToken, string email)
    {
        var customerClient = new CustomerClient();

        // Buscar ou criar customer
        var searchRequest = new SearchRequest { Filters = new Dictionary<string, object> { { "email", email } } };
        var searchResult = await customerClient.SearchAsync(searchRequest);
        Customer customer;

        if (searchResult.Results.Count > 0)
        {
            customer = searchResult.Results[0];
        }
        else
        {
            customer = await customerClient.CreateAsync(new CustomerRequest { Email = email });
        }

        // Salvar cartão no customer
        var cardResult = await customerClient.CreateCardAsync(customer.Id, new CustomerCardCreateRequest
        {
            Token = cardToken
        });

        _logger.LogInformation("Cartão salvo: customer={CustomerId} card={CardId}", customer.Id, cardResult.Id);
        return (customer.Id, cardResult.Id);
    }


    public async Task<PaymentResult> CreateSavedCardPaymentAsync(SavedCardPaymentRequest request)
    {
        var client = new PaymentClient();

        var paymentRequest = new PaymentCreateRequest
        {
            TransactionAmount = request.amount,
            Description = request.description,
            ExternalReference = request.external_reference,
            Installments = 1,
            Payer = new PaymentPayerRequest
            {
                Type = "customer",
                Id = request.customer_id
            },
            Token = request.card_id
        };

        var payment = await client.CreateAsync(paymentRequest);
        _logger.LogInformation("Pagamento com cartão salvo: {Id} status={Status}", payment.Id, payment.Status);

        return new PaymentResult
        {
            payment_id = payment.Id?.ToString() ?? "",
            status = payment.Status ?? "unknown",
            status_detail = payment.StatusDetail ?? ""
        };
    }
}
