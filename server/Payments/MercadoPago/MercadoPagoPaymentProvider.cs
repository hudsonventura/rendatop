using MercadoPago.Client;
using MercadoPago.Client.Customer;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Customer;
using MercadoPago.Resource.Payment;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

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
        var syntheticAddress = BuildSyntheticBoletoAddress();

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
                },
                Address = syntheticAddress
            }
        };

        var payment = await client.CreateAsync(paymentRequest);
        _logger.LogInformation("Pagamento boleto criado: {Id} status={Status}", payment.Id, payment.Status);

        string? barcodeContent = null;
        string? digitableLine = null;
        try { barcodeContent = payment.TransactionDetails?.Barcode?.Content; } catch { }
        try { digitableLine = payment.TransactionDetails?.DigitableLine; } catch { }
        var barcodeImageBase64 = GenerateBoletoBarcodeImageBase64FromDigitableLine(digitableLine);

        return new PaymentResult
        {
            payment_id = payment.Id?.ToString() ?? "",
            status = payment.Status ?? "unknown",
            status_detail = payment.StatusDetail ?? "",
            boleto_barcode_content = barcodeContent,
            boleto_barcode_image_base64 = barcodeImageBase64,
            boleto_digitable_line = digitableLine,
            boleto_url = payment.TransactionDetails?.ExternalResourceUrl
        };
    }

    private string? GenerateBoletoBarcodeImageBase64FromDigitableLine(string? digitableLine)
    {
        var barcodeContent = ConvertDigitableLineToBarcode(digitableLine);
        if (string.IsNullOrWhiteSpace(barcodeContent))
            return null;

        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.ITF,
            Options = new EncodingOptions
            {
                Width = 2200,
                Height = 320,
                Margin = 40,
                PureBarcode = true
            }
        };

        using var bitmap = writer.Write(barcodeContent);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return Convert.ToBase64String(data.ToArray());
    }

    private string? ConvertDigitableLineToBarcode(string? digitableLine)
    {
        if (string.IsNullOrWhiteSpace(digitableLine))
            return null;

        var digits = new string(digitableLine.Where(char.IsDigit).ToArray());

        // Boleto bancario: 47-digit line -> 44-digit barcode representation.
        if (digits.Length == 47)
        {
            return string.Concat(
                digits[..4],
                digits[32],
                digits[33..47],
                digits[4..9],
                digits[10..20],
                digits[21..31]
            );
        }

        return digits.Length == 44 ? digits : null;
    }

    private global::MercadoPago.Client.Payment.PaymentPayerAddressRequest BuildSyntheticBoletoAddress()
    {
        var presets = new[]
        {
            new
            {
                State = "SP",
                City = "Sao Paulo",
                Neighborhoods = new[] { "Centro", "Bela Vista", "Pinheiros" },
                Streets = new[] { "Rua das Flores", "Avenida Brasil", "Rua Augusta" },
                ZipPrefixes = new[] { "01001", "01310", "05422" }
            },
            new
            {
                State = "RJ",
                City = "Rio de Janeiro",
                Neighborhoods = new[] { "Copacabana", "Botafogo", "Centro" },
                Streets = new[] { "Avenida Atlantica", "Rua Voluntarios da Patria", "Rua do Catete" },
                ZipPrefixes = new[] { "22010", "22250", "20031" }
            },
            new
            {
                State = "MG",
                City = "Belo Horizonte",
                Neighborhoods = new[] { "Savassi", "Centro", "Funcionarios" },
                Streets = new[] { "Avenida Afonso Pena", "Rua da Bahia", "Rua Pernambuco" },
                ZipPrefixes = new[] { "30130", "30160", "30150" }
            }
        };

        var preset = presets[Random.Shared.Next(presets.Length)];
        var zipPrefix = preset.ZipPrefixes[Random.Shared.Next(preset.ZipPrefixes.Length)];

        return new global::MercadoPago.Client.Payment.PaymentPayerAddressRequest
        {
            ZipCode = $"{zipPrefix}{Random.Shared.Next(100, 999)}",
            StreetName = preset.Streets[Random.Shared.Next(preset.Streets.Length)],
            StreetNumber = Random.Shared.Next(10, 9999),
            Neighborhood = preset.Neighborhoods[Random.Shared.Next(preset.Neighborhoods.Length)],
            City = preset.City,
            FederalUnit = preset.State
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
