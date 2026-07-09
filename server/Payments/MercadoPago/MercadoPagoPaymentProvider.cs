using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MercadoPago.Client.AuthorizedPayment;
using MercadoPago.Client;
using MercadoPago.Client.Customer;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Client.Preapproval;
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
    private readonly string _accessToken;
    private readonly string _statementDescriptor;

    public MercadoPagoPaymentProvider(ILogger<MercadoPagoPaymentProvider> logger)
    {
        _logger = logger;
        string? accessToken = Environment.GetEnvironmentVariable("MERCADO_PAGO_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("MERCADO_PAGO_ACCESS_TOKEN não configurado.");

        // Log para debug — remover em produção
        var trimmed = accessToken.Trim();

        _accessToken = trimmed;
        MercadoPagoConfig.AccessToken = trimmed;

        _statementDescriptor = BuildStatementDescriptor(
            Environment.GetEnvironmentVariable("MERCADO_PAGO_STATEMENT_DESCRIPTOR"));
        _logger.LogInformation("MP statement descriptor configurado: {StatementDescriptor} {_tags_}", _statementDescriptor, _tags);
    }

    public async Task<PaymentResult> CreateHostedSubscriptionAsync(HostedSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("criar a assinatura hospedada", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Criando preapproval hospedado no Mercado Pago. TraceId={TraceId} Payload={@Payload} Tags={_tags_}",
                traceId,
                new
                {
                    request.amount,
                    request.description,
                    request.payer_email,
                    request.external_reference,
                    request.back_url,
                    request.notification_url,
                    request.start_date,
                    request.end_date
                },
                _tags);

            var client = new PreapprovalClient();
            var preapproval = await client.CreateAsync(new PreapprovalCreateRequest
            {
                Reason = request.description,
                ExternalReference = request.external_reference,
                PayerEmail = request.payer_email,
                BackUrl = request.back_url,
                Status = "pending",
                AutoRecurring = new PreApprovalAutoRecurringCreateRequest
                {
                    Frequency = 1,
                    FrequencyType = "months",
                    TransactionAmount = request.amount,
                    CurrencyId = "BRL",
                    StartDate = UtcDateTime.EnsureUtc(request.start_date),
                    EndDate = UtcDateTime.EnsureUtc(request.end_date)
                }
            }, new RequestOptions
            {
                CustomHeaders =
                {
                    { "X-Notification-URL", request.notification_url }
                }
            }, cancellationToken);

            _logger.LogInformation(
                "Preapproval criado no Mercado Pago. TraceId={TraceId} PreapprovalId={PreapprovalId} Status={Status} ExternalReference={ExternalReference} Tags={_tags_}",
                traceId,
                preapproval.Id,
                preapproval.Status,
                preapproval.ExternalReference,
                _tags);

            return new PaymentResult
            {
                status = preapproval.Status ?? "pending",
                status_detail = preapproval.Status ?? "pending",
                payment_method = preapproval.PaymentMethodId,
                amount = preapproval.AutoRecurring?.TransactionAmount,
                preapproval_id = preapproval.Id,
                external_reference = preapproval.ExternalReference,
                checkout_url = preapproval.InitPoint ?? preapproval.SandboxInitPoint
            };
        });
    }

    public async Task<PaymentResult> CreateHostedCheckoutPreferenceAsync(HostedCheckoutPreferenceRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("criar o checkout hospedado", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            _logger.LogInformation(
                "Criando preference do Checkout Pro no Mercado Pago. TraceId={TraceId} Payload={@Payload} Tags={_tags_}",
                traceId,
                new
                {
                    request.title,
                    request.amount,
                    request.external_reference,
                    request.payment_method,
                    request.success_url,
                    request.pending_url,
                    request.failure_url,
                    request.notification_url,
                    request.date_of_expiration
                },
                _tags);

            var client = new PreferenceClient();
            var preferenceRequest = BuildHostedCheckoutPreferenceRequest(request, includeStatementDescriptor: true);
            global::MercadoPago.Resource.Preference.Preference preference;

            try
            {
                preference = await client.CreateAsync(preferenceRequest, cancellationToken: cancellationToken);
            }
            catch (MercadoPagoException ex) when (ShouldRetryHostedCheckoutPreference(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Falha generica do SDK ao criar preference do Checkout Pro. TraceId={TraceId} PaymentMethod={PaymentMethod} ExternalReference={ExternalReference} Tentando fallback sem statement descriptor. Tags={_tags_}",
                    traceId,
                    request.payment_method,
                    request.external_reference,
                    _tags);

                var fallbackRequest = BuildHostedCheckoutPreferenceRequest(request, includeStatementDescriptor: false);
                preference = await client.CreateAsync(fallbackRequest, cancellationToken: cancellationToken);
            }

            _logger.LogInformation(
                "Preference do Checkout Pro criada. TraceId={TraceId} PreferenceId={PreferenceId} ExternalReference={ExternalReference} Tags={_tags_}",
                traceId,
                preference.Id,
                preference.ExternalReference,
                _tags);

            return new PaymentResult
            {
                status = "pending",
                status_detail = "checkout_preference_created",
                payment_id = string.Empty,
                external_reference = preference.ExternalReference,
                checkout_url = preference.InitPoint ?? preference.SandboxInitPoint,
                amount = request.amount,
                payment_method = request.payment_method,
                preference_id = preference.Id
            };
        });
    }

    private PreferenceRequest BuildHostedCheckoutPreferenceRequest(
        HostedCheckoutPreferenceRequest request,
        bool includeStatementDescriptor)
    {
        return new PreferenceRequest
        {
            Items =
            [
                new PreferenceItemRequest
                {
                    Id = request.external_reference,
                    Title = request.title,
                    Description = request.description,
                    Quantity = 1,
                    CurrencyId = "BRL",
                    UnitPrice = request.amount
                }
            ],
            Payer = new PreferencePayerRequest
            {
                Email = request.payer_email
            },
            PaymentMethods = BuildPreferencePaymentMethods(request.payment_method),
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = request.success_url,
                Pending = request.pending_url,
                Failure = request.failure_url
            },
            NotificationUrl = request.notification_url,
            StatementDescriptor = includeStatementDescriptor ? _statementDescriptor : null,
            ExternalReference = request.external_reference,
            Expires = request.date_of_expiration.HasValue,
            DateOfExpiration = UtcDateTime.EnsureUtc(request.date_of_expiration),
            AutoReturn = "approved"
        };
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

    public async Task<PaymentResult> GetSubscriptionStatusAsync(string preapprovalId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("consultar o status da assinatura", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            var client = new PreapprovalClient();
            var preapproval = await client.GetAsync(preapprovalId, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Status da assinatura consultado no Mercado Pago. TraceId={TraceId} PreapprovalId={PreapprovalId} Status={Status} ExternalReference={ExternalReference} Tags={_tags_}",
                traceId,
                preapproval.Id,
                preapproval.Status,
                preapproval.ExternalReference,
                _tags);

            return new PaymentResult
            {
                status = preapproval.Status ?? "unknown",
                status_detail = preapproval.Status ?? "unknown",
                payment_method = preapproval.PaymentMethodId,
                amount = preapproval.AutoRecurring?.TransactionAmount,
                preapproval_id = preapproval.Id,
                external_reference = preapproval.ExternalReference,
                checkout_url = preapproval.InitPoint ?? preapproval.SandboxInitPoint
            };
        });
    }

    public async Task<PaymentResult?> FindPaymentByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalReference))
            return null;

        var traceId = TraceContext.GetTraceId();
        var url = $"https://api.mercadopago.com/v1/payments/search?external_reference={Uri.EscapeDataString(externalReference)}&sort=date_created&criteria=desc&limit=1";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        _logger.LogInformation(
            "Consultando pagamento por external_reference no Mercado Pago. TraceId={TraceId} ExternalReference={ExternalReference} Tags={_tags_}",
            traceId,
            externalReference,
            _tags);

        using var response = await httpClient.GetAsync(url, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Mercado Pago recusou a consulta por external_reference. TraceId={TraceId} ExternalReference={ExternalReference} StatusCode={StatusCode} Response={Response} Tags={_tags_}",
                traceId,
                externalReference,
                (int)response.StatusCode,
                responseBody,
                _tags);
            throw new ExpectedException(
                $"Mercado Pago não conseguiu consultar o pagamento pela referência externa. status_http={(int)response.StatusCode}",
                HttpStatusCode.BadGateway);
        }

        using var json = JsonDocument.Parse(responseBody);
        if (!json.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            _logger.LogInformation(
                "Nenhum pagamento encontrado para external_reference no Mercado Pago. TraceId={TraceId} ExternalReference={ExternalReference} Tags={_tags_}",
                traceId,
                externalReference,
                _tags);
            return null;
        }

        var result = MapPaymentSearchResult(results[0], externalReference);

        _logger.LogInformation(
            "Pagamento localizado por external_reference. TraceId={TraceId} ExternalReference={ExternalReference} PaymentId={PaymentId} Status={Status} Tags={_tags_}",
            traceId,
            externalReference,
            result.payment_id,
            result.status,
            _tags);

        return result;
    }

    public async Task<PaymentResult> GetAuthorizedPaymentStatusAsync(string authorizedPaymentId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithMercadoPagoHandlingAsync("consultar o pagamento autorizado da assinatura", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            var client = new AuthorizedPaymentClient();
            var authorizedPayment = await client.GetAsync(long.Parse(authorizedPaymentId), cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Pagamento autorizado consultado no Mercado Pago. TraceId={TraceId} AuthorizedPaymentId={AuthorizedPaymentId} PreapprovalId={PreapprovalId} Status={Status} Tags={_tags_}",
                traceId,
                authorizedPayment.Id,
                authorizedPayment.PreapprovalId,
                authorizedPayment.Status,
                _tags);

            return new PaymentResult
            {
                payment_id = authorizedPayment.Payment?.Id?.ToString() ?? authorizedPayment.Id.ToString(),
                status = authorizedPayment.Payment?.Status ?? authorizedPayment.Status ?? "unknown",
                status_detail = authorizedPayment.Payment?.StatusDetail ?? authorizedPayment.Status ?? "unknown",
                payment_method = null,
                amount = authorizedPayment.TransactionAmount,
                approved_at = authorizedPayment.DebitDate?.UtcDateTime,
                date_of_expiration = authorizedPayment.DebitDate?.UtcDateTime,
                preapproval_id = authorizedPayment.PreapprovalId,
                external_reference = authorizedPayment.ExternalReference
            };
        });
    }

    public async Task CancelSubscriptionAsync(string preapprovalId, CancellationToken cancellationToken = default)
    {
        await ExecuteWithMercadoPagoHandlingAsync("cancelar a assinatura hospedada", async () =>
        {
            var traceId = TraceContext.GetTraceId();
            var client = new PreapprovalClient();
            await client.UpdateAsync(preapprovalId, new PreapprovalUpdateRequest
            {
                Status = "cancelled"
            }, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Assinatura hospedada cancelada no Mercado Pago. TraceId={TraceId} PreapprovalId={PreapprovalId} Tags={_tags_}",
                traceId,
                preapprovalId,
                _tags);
            return true;
        });
    }

    private PreferencePaymentMethodsRequest BuildPreferencePaymentMethods(string paymentMethod)
    {
        var excludedTypes = new List<PreferencePaymentTypeRequest>();

        if (string.Equals(paymentMethod, "pix", StringComparison.OrdinalIgnoreCase))
        {
            excludedTypes.Add(new PreferencePaymentTypeRequest { Id = "ticket" });
            excludedTypes.Add(new PreferencePaymentTypeRequest { Id = "credit_card" });
            excludedTypes.Add(new PreferencePaymentTypeRequest { Id = "debit_card" });
        }
        else if (string.Equals(paymentMethod, "boleto", StringComparison.OrdinalIgnoreCase))
        {
            excludedTypes.Add(new PreferencePaymentTypeRequest { Id = "bank_transfer" });
            excludedTypes.Add(new PreferencePaymentTypeRequest { Id = "credit_card" });
            excludedTypes.Add(new PreferencePaymentTypeRequest { Id = "debit_card" });
        }

        return new PreferencePaymentMethodsRequest
        {
            ExcludedPaymentTypes = excludedTypes
        };
    }

    private static bool ShouldRetryHostedCheckoutPreference(MercadoPagoException exception)
    {
        var message = BuildProviderExceptionMessage(exception);
        return message.Contains("unexpected error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unexpected error has occurred", StringComparison.OrdinalIgnoreCase);
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

    private static PaymentResult MapPaymentSearchResult(JsonElement payment, string externalReference)
    {
        return new PaymentResult
        {
            payment_id = ReadJsonString(payment, "id") ?? string.Empty,
            status = ReadJsonString(payment, "status") ?? "unknown",
            status_detail = ReadJsonString(payment, "status_detail") ?? string.Empty,
            payment_method = ReadJsonString(payment, "payment_method_id"),
            amount = ReadJsonDecimal(payment, "transaction_amount"),
            approved_at = ReadJsonDateTime(payment, "date_approved"),
            date_of_expiration = ReadJsonDateTime(payment, "date_of_expiration"),
            external_reference = ReadJsonString(payment, "external_reference") ?? externalReference,
            pix_qr_code = ReadNestedJsonString(payment, "point_of_interaction", "transaction_data", "qr_code"),
            pix_qr_code_base64 = ReadNestedJsonString(payment, "point_of_interaction", "transaction_data", "qr_code_base64"),
            boleto_barcode_content = ReadNestedJsonString(payment, "transaction_details", "barcode", "content"),
            boleto_digitable_line = ReadNestedJsonString(payment, "transaction_details", "digitable_line"),
            boleto_url = ReadNestedJsonString(payment, "transaction_details", "external_resource_url")
        };
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }

    private static decimal? ReadJsonDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
            return decimalValue;

        if (property.ValueKind == JsonValueKind.String &&
            decimal.TryParse(property.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTime? ReadJsonDateTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        return DateTime.TryParse(
            property.GetString(),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static string? ReadNestedJsonString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (!current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.ToString(),
            _ => null
        };
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
                $"Falha ao comunicar com o Mercado Pago ao {operation}. {BuildProviderExceptionMessage(ex)}",
                HttpStatusCode.BadGateway);
        }
    }

    private static string BuildProviderExceptionMessage(Exception exception)
    {
        var parts = new List<string>();
        var current = exception;

        while (current != null)
        {
            var message = SanitizeProviderText(current.Message);
            if (!string.IsNullOrWhiteSpace(message) && !parts.Contains(message, StringComparer.OrdinalIgnoreCase))
                parts.Add(message);

            current = current.InnerException!;
        }

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "O provedor não detalhou o motivo.";
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
