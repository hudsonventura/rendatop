using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MercadoPago.Client;
using MercadoPago.Client.Customer;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Error;
using MercadoPago.Resource.Customer;
using MercadoPago.Resource.Payment;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using server.Utils;

namespace server.Payments.MercadoPago;

/// <summary>
/// Implementação do IPaymentProvider usando a API do Mercado Pago.
/// Usa a Payments API (/v1/payments) para transparência total no checkout.
/// </summary>
public class MercadoPagoPaymentProvider : IPaymentProvider
{
    private readonly ILogger<MercadoPagoPaymentProvider> _logger;
    private readonly List<string> _tags = new() { "MercadoPagoPaymentProvider", "Payments", "MercadoPago" };
    private readonly string _statementDescriptor;

    public MercadoPagoPaymentProvider(ILogger<MercadoPagoPaymentProvider> logger)
    {
        _logger = logger;
        string? accessToken = Environment.GetEnvironmentVariable("MERCADO_PAGO_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("MERCADO_PAGO_ACCESS_TOKEN não configurado.");

        // Log para debug — remover em produção
        var trimmed = accessToken.Trim();

        MercadoPagoConfig.AccessToken = trimmed;

        _statementDescriptor = BuildStatementDescriptor(
            Environment.GetEnvironmentVariable("MERCADO_PAGO_STATEMENT_DESCRIPTOR"));
        _logger.LogInformation("MP statement descriptor configurado: {StatementDescriptor} {_tags_}", _statementDescriptor, _tags);
    }


    public async Task<PaymentResult> CreateCardPaymentAsync(CardPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.payment_method_id))
            throw new ExpectedException("Não foi possível identificar a bandeira do cartão. Confira o número informado e tente novamente.");

        return await ExecuteWithMercadoPagoHandlingAsync("processar o pagamento com cartão", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Enviando pagamento com cartão ao Mercado Pago. TraceId={TraceId} Payload={@Payload} Tags={_tags_}",
                traceId,
                new
                {
                    request.amount,
                    request.description,
                    request.payment_method_id,
                    request.card_type,
                    request.installments,
                    request.issuer_id,
                    request.payer_email,
                    request.external_reference
                }, _tags);
            var client = new PaymentClient();

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.amount,
                Token = request.card_token,
                Description = request.description,
                StatementDescriptor = _statementDescriptor,
                Installments = request.installments,
                PaymentMethodId = request.payment_method_id,
                ExternalReference = request.external_reference,
                PaymentMethod = new PaymentMethodRequest
                {
                    Type = request.card_type
                },
                Payer = new PaymentPayerRequest
                {
                    Email = request.payer_email
                }
            };

            if (!string.IsNullOrEmpty(request.issuer_id))
                paymentRequest.IssuerId = request.issuer_id;

            var payment = await client.CreateAsync(paymentRequest);
            _logger.LogInformation(
                "Pagamento com cartão criado no Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
                traceId,
                payment.Id,
                payment.Status,
                payment.StatusDetail,
                _tags);

            return new PaymentResult
            {
                payment_id = payment.Id?.ToString() ?? "",
                status = payment.Status ?? "unknown",
                status_detail = payment.StatusDetail ?? "",
                payment_method = payment.PaymentMethodId,
                amount = payment.TransactionAmount,
                approved_at = UtcDateTime.EnsureUtc(payment.DateApproved),
                date_of_expiration = UtcDateTime.EnsureUtc(payment.DateOfExpiration)
            };
        });
    }


    public async Task<PaymentResult> CreatePixPaymentAsync(PixPaymentRequest request)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("gerar o pagamento PIX", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Enviando geracao de PIX ao Mercado Pago. TraceId={TraceId} Payload={@Payload} Tags={_tags_}",
                traceId,
                new
                {
                    request.amount,
                    request.description,
                    request.payer_email,
                    request.payer_first_name,
                    request.payer_last_name,
                    request.payer_cpf,
                    request.external_reference,
                    request.date_of_expiration
                }, _tags);
            var client = new PaymentClient();

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.amount,
                Description = request.description,
                PaymentMethodId = "pix",
                ExternalReference = request.external_reference,
                DateOfExpiration = request.date_of_expiration,
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
            _logger.LogInformation(
                "Pagamento PIX criado no Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
                traceId,
                payment.Id,
                payment.Status,
                payment.StatusDetail,
                _tags);

            var pointOfInteraction = payment.PointOfInteraction;
            var transactionData = pointOfInteraction?.TransactionData;

            return new PaymentResult
            {
                payment_id = payment.Id?.ToString() ?? "",
                status = payment.Status ?? "unknown",
                status_detail = payment.StatusDetail ?? "",
                payment_method = payment.PaymentMethodId,
                amount = payment.TransactionAmount,
                approved_at = UtcDateTime.EnsureUtc(payment.DateApproved),
                date_of_expiration = UtcDateTime.EnsureUtc(payment.DateOfExpiration),
                pix_qr_code = transactionData?.QrCode,
                pix_qr_code_base64 = transactionData?.QrCodeBase64
            };
        });
    }


    public async Task<PaymentResult> CreateBoletoPaymentAsync(BoletoPaymentRequest request)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("gerar o boleto", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Enviando geracao de boleto ao Mercado Pago. TraceId={TraceId} Payload={@Payload} Tags={_tags_}",
                traceId,
                new
                {
                    request.amount,
                    request.description,
                    request.payer_email,
                    request.payer_first_name,
                    request.payer_last_name,
                    request.payer_cpf,
                    request.external_reference,
                    request.date_of_expiration
                }, _tags);
            var client = new PaymentClient();
            var syntheticAddress = BuildSyntheticBoletoAddress();

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.amount,
                Description = request.description,
                PaymentMethodId = "bolbradesco",
                ExternalReference = request.external_reference,
                DateOfExpiration = request.date_of_expiration,
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
            _logger.LogInformation(
                "Pagamento boleto criado no Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
                traceId,
                payment.Id,
                payment.Status,
                payment.StatusDetail,
                _tags);

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
                payment_method = payment.PaymentMethodId,
                amount = payment.TransactionAmount,
                approved_at = UtcDateTime.EnsureUtc(payment.DateApproved),
                date_of_expiration = UtcDateTime.EnsureUtc(payment.DateOfExpiration),
                boleto_barcode_content = barcodeContent,
                boleto_barcode_image_base64 = barcodeImageBase64,
                boleto_digitable_line = digitableLine,
                boleto_url = payment.TransactionDetails?.ExternalResourceUrl
            };
        });
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
        return await ExecuteWithMercadoPagoHandlingAsync("consultar o status do pagamento", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Consultando status no Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} Tags={_tags_}",
                traceId,
                paymentId, _tags);
            var client = new PaymentClient();
            var payment = await client.GetAsync(long.Parse(paymentId));

            _logger.LogInformation(
                "Status consultado no Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
                traceId,
                payment.Id,
                payment.Status,
                payment.StatusDetail,
                _tags);

            return new PaymentResult
            {
                payment_id = payment.Id?.ToString() ?? "",
                status = payment.Status ?? "unknown",
                status_detail = payment.StatusDetail ?? "",
                payment_method = payment.PaymentMethodId,
                amount = payment.TransactionAmount,
                approved_at = UtcDateTime.EnsureUtc(payment.DateApproved),
                date_of_expiration = UtcDateTime.EnsureUtc(payment.DateOfExpiration),
                pix_qr_code = payment.PointOfInteraction?.TransactionData?.QrCode,
                pix_qr_code_base64 = payment.PointOfInteraction?.TransactionData?.QrCodeBase64,
                boleto_barcode_content = payment.TransactionDetails?.Barcode?.Content,
                boleto_digitable_line = payment.TransactionDetails?.DigitableLine,
                boleto_url = payment.TransactionDetails?.ExternalResourceUrl
            };
        });
    }


    public async Task<(string customerId, string cardId)> SaveCardAsync(string cardToken, string email)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("salvar o cartão para cobranças futuras", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Salvando cartao para renovacao automatica no Mercado Pago. TraceId={TraceId} Email={Email} Tags={_tags_}",
                traceId,
                email, _tags);
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

            _logger.LogInformation(
                "Cartao salvo no Mercado Pago. TraceId={TraceId} CustomerId={CustomerId} CardId={CardId} Tags={_tags_}",
                traceId,
                customer.Id,
                cardResult.Id, _tags);
            return (customer.Id, cardResult.Id);
        });
    }


    public async Task<PaymentResult> CreateSavedCardPaymentAsync(SavedCardPaymentRequest request)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("processar a renovação automática no cartão", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Enviando renovacao automatica com cartao salvo ao Mercado Pago. TraceId={TraceId} Payload={@Payload} Tags={_tags_}",
                traceId,
                new
                {
                    request.customer_id,
                    request.card_id,
                    request.amount,
                    request.description,
                    request.external_reference
                }, _tags);
            var client = new PaymentClient();

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.amount,
                Description = request.description,
                StatementDescriptor = _statementDescriptor,
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
            _logger.LogInformation(
                "Pagamento com cartao salvo criado no Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} Status={Status} StatusDetail={StatusDetail} Tags={_tags_}",
                traceId,
                payment.Id,
                payment.Status,
                payment.StatusDetail,
                _tags);

            return new PaymentResult
            {
                payment_id = payment.Id?.ToString() ?? "",
                status = payment.Status ?? "unknown",
                status_detail = payment.StatusDetail ?? "",
                payment_method = payment.PaymentMethodId,
                amount = payment.TransactionAmount,
                approved_at = UtcDateTime.EnsureUtc(payment.DateApproved),
                date_of_expiration = UtcDateTime.EnsureUtc(payment.DateOfExpiration)
            };
        });
    }

    private static string BuildStatementDescriptor(string? rawValue)
    {
        const string fallback = "RENDATOP";

        var value = string.IsNullOrWhiteSpace(rawValue) ? fallback : rawValue.Trim();
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = char.GetUnicodeCategory(ch);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch) || ch == ' ')
                builder.Append(char.ToUpperInvariant(ch));
        }

        var sanitized = Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = fallback;

        if (sanitized.Length > 22)
            sanitized = sanitized[..22].TrimEnd();

        return sanitized;
    }

    public async Task<PaymentRefundResult> RefundPaymentAsync(string paymentId, decimal? amount = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("solicitar o estorno do pagamento", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Solicitando estorno ao Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} Amount={Amount} Tags={_tags_}",
                traceId,
                paymentId,
                amount,
                _tags);
            var client = new PaymentRefundClient();
            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("X-Render-In-Process-Refunds", "true");
            var refund = await client.RefundAsync(long.Parse(paymentId), amount, requestOptions, cancellationToken);

            var result = new PaymentRefundResult
            {
                refund_id = refund.Id?.ToString() ?? string.Empty,
                status = refund.Status ?? string.Empty,
                amount = refund.Amount,
                created_at = UtcDateTime.EnsureUtc(refund.DateCreated)
            };

            _logger.LogInformation(
                "Estorno solicitado com sucesso no Mercado Pago. TraceId={TraceId} PaymentId={PaymentId} RefundId={RefundId} Status={Status} Tags={_tags_}",
                traceId,
                paymentId,
                result.refund_id,
                result.status,
                _tags);

            return result;
        });
    }

    private async Task<T> ExecuteWithMercadoPagoHandlingAsync<T>(string operation, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (MercadoPagoApiException ex)
        {
            _logger.LogWarning(
                ex,
                "Erro da API do Mercado Pago ao {Operation}. TraceId={TraceId} StatusCode={StatusCode} ApiError={ApiError} Tags={_tags_}",
                operation,
                TraceContext.GetTraceId(),
                ex.StatusCode,
                SafeSerialize(ex.ApiError),
                _tags);
            throw new ExpectedException(FormatMercadoPagoApiException(operation, ex));
        }
        catch (MercadoPagoException ex)
        {
            _logger.LogError(
                ex,
                "Erro do SDK do Mercado Pago ao {Operation}. TraceId={TraceId} Tags={_tags_}",
                operation,
                TraceContext.GetTraceId(),
                _tags);
            throw new ExpectedException(
                $"Falha ao comunicar com o Mercado Pago ao {operation}. {SanitizeProviderText(ex.Message)}",
                HttpStatusCode.BadGateway);
        }
    }

    private static string FormatMercadoPagoApiException(string operation, MercadoPagoApiException exception)
    {
        var parts = new List<string>();
        var apiError = exception.ApiError;

        var apiMessage = SanitizeProviderText(apiError?.Message);
        if (!string.IsNullOrWhiteSpace(apiMessage))
            parts.Add(apiMessage);

        var apiErrorCode = SanitizeProviderText(apiError?.Error);
        if (!string.IsNullOrWhiteSpace(apiErrorCode))
            parts.Add($"erro={apiErrorCode}");

        var apiStatus = Convert.ToString(apiError?.Status);
        if (!string.IsNullOrWhiteSpace(apiStatus))
            parts.Add($"status_http={apiStatus}");

        var causeMessages = apiError?.Cause?
            .Select(FormatMercadoPagoCause)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToArray()
            ?? [];

        if (causeMessages.Length > 0)
            parts.Add($"causas: {string.Join(" | ", causeMessages)}");

        if (parts.Count == 0)
        {
            var fallback = SanitizeProviderText(exception.Message);
            if (!string.IsNullOrWhiteSpace(fallback))
                parts.Add(fallback);
        }

        var detail = parts.Count > 0
            ? string.Join(". ", parts)
            : "O provedor recusou a operação sem detalhar o motivo.";

        return $"Mercado Pago não conseguiu {operation}. Entre em contato pelo email contato@rendatop.com.br  {detail}";
    }

    private static string FormatMercadoPagoCause(ApiErrorCause cause)
    {
        var pieces = new List<string>();
        var code = SanitizeProviderText(cause.Code);
        if (!string.IsNullOrWhiteSpace(code))
            pieces.Add($"code={code}");

        var primaryMessage = FirstNonEmpty(cause.Description, cause.Message, cause.Details);
        if (!string.IsNullOrWhiteSpace(primaryMessage))
            pieces.Add(primaryMessage);

        var details = SanitizeProviderText(cause.Details);
        if (!string.IsNullOrWhiteSpace(details) &&
            !string.Equals(details, primaryMessage, StringComparison.OrdinalIgnoreCase))
        {
            pieces.Add($"details={details}");
        }

        return string.Join(" - ", pieces);
    }

    private static string SafeSerialize(object? value)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return "<serialization_error>";
        }
    }

    private static string? FirstNonEmpty(params object?[] values)
    {
        foreach (var value in values)
        {
            var sanitized = SanitizeProviderText(value);
            if (!string.IsNullOrWhiteSpace(sanitized))
                return sanitized;
        }

        return null;
    }

    private static string SanitizeProviderText(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is string text)
            return text.Replace('\n', ' ').Replace('\r', ' ').Trim();

        if (value is System.Collections.IEnumerable values)
        {
            var items = new List<string>();
            foreach (var item in values)
            {
                var sanitizedItem = SanitizeProviderText(item);
                if (!string.IsNullOrWhiteSpace(sanitizedItem))
                    items.Add(sanitizedItem);
            }

            return string.Join(" | ", items.Distinct());
        }

        return Convert.ToString(value)?.Replace('\n', ' ').Replace('\r', ' ').Trim() ?? string.Empty;
    }
}
