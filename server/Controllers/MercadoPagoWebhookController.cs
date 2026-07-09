using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Services;
using server.Utils;

namespace server.Controllers;

[ApiController]
[AllowAnonymous]
public class MercadoPagoWebhookController : ControllerBase
{
    private readonly SubscriptionBillingService _billing;
    private readonly ILogger<MercadoPagoWebhookController> _logger;
    private readonly List<string> _tags = new() { "MercadoPagoWebhookController", "Webhook", "MercadoPago" };
    private readonly string? _clientBaseUrl = Environment.GetEnvironmentVariable("BASE_URL_CLIENT");

    public MercadoPagoWebhookController(
        SubscriptionBillingService billing,
        ILogger<MercadoPagoWebhookController> logger)
    {
        _billing = billing;
        _logger = logger;
    }

    [HttpGet("subscription/webhook/mercado-pago")]
    public IActionResult ReturnFromLegacyWebhookUrl()
    {
        if (string.IsNullOrWhiteSpace(_clientBaseUrl))
            throw new ExpectedException("BASE_URL_CLIENT não configurado para retorno do checkout.");

        if (!Uri.TryCreate(_clientBaseUrl.Trim(), UriKind.Absolute, out var clientBaseUri))
            throw new ExpectedException("BASE_URL_CLIENT configurado de forma inválida para retorno do checkout.");

        var destination = $"{clientBaseUri.ToString().TrimEnd('/')}/subscription/mercado-pago/return{Request.QueryString}";

        _logger.LogWarning(
            "Retorno de navegador recebido na URL de webhook do Mercado Pago. TraceId={TraceId} RedirectingTo={RedirectingTo} Tags={_tags_}",
            TraceContext.GetTraceId(),
            destination,
            _tags);

        return Redirect(destination);
    }

    [HttpPost("subscription/webhook/mercado-pago")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var secret = Environment.GetEnvironmentVariable("MERCADO_PAGO_WEBHOOK_SECRET");
        var xSignature = Request.Headers["x-signature"].ToString();
        var xRequestId = Request.Headers["x-request-id"].ToString();
        var queryDataId = Request.Query["data.id"].ToString();

        if (!string.IsNullOrWhiteSpace(secret))
        {
            ValidateWebhookSignature(secret, xSignature, xRequestId, queryDataId);
        }

        using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var topic = GetString(root, "type") ?? Request.Query["type"].ToString() ?? Request.Query["topic"].ToString();
        var dataId = queryDataId;

        if (string.IsNullOrWhiteSpace(dataId) &&
            root.TryGetProperty("data", out var dataElement) &&
            dataElement.TryGetProperty("id", out var idElement))
        {
            dataId = idElement.GetString() ?? idElement.ToString();
        }

        if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(dataId))
        {
            _logger.LogWarning(
                "Webhook do Mercado Pago ignorado por falta de topico ou id. TraceId={TraceId} Topic={Topic} DataId={DataId} Tags={_tags_}",
                TraceContext.GetTraceId(),
                topic,
                dataId,
                _tags);
            return Ok();
        }

        _logger.LogInformation(
            "Webhook do Mercado Pago recebido. TraceId={TraceId} Topic={Topic} DataId={DataId} Tags={_tags_}",
            TraceContext.GetTraceId(),
            topic,
            dataId,
            _tags);

        switch (topic)
        {
            case "payment":
                await _billing.HandleMercadoPagoPaymentWebhookAsync(dataId, cancellationToken);
                break;
            case "subscription_preapproval":
                await _billing.HandleMercadoPagoSubscriptionWebhookAsync(dataId, cancellationToken);
                break;
            case "subscription_authorized_payment":
                await _billing.HandleMercadoPagoAuthorizedPaymentWebhookAsync(dataId, cancellationToken);
                break;
        }

        return Ok();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            ? property.GetString() ?? property.ToString()
            : null;
    }

    private static void ValidateWebhookSignature(string secret, string xSignature, string xRequestId, string dataId)
    {
        if (string.IsNullOrWhiteSpace(xSignature) || string.IsNullOrWhiteSpace(xRequestId) || string.IsNullOrWhiteSpace(dataId))
            throw new ExpectedException("Webhook do Mercado Pago sem assinatura válida.", System.Net.HttpStatusCode.Unauthorized);

        var parts = xSignature
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("ts", out var ts) || !parts.TryGetValue("v1", out var providedHash))
        {
            throw new ExpectedException("Webhook do Mercado Pago sem assinatura válida.", System.Net.HttpStatusCode.Unauthorized);
        }

        var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(providedHash.ToLowerInvariant())))
        {
            throw new ExpectedException("Assinatura do webhook do Mercado Pago inválida.", System.Net.HttpStatusCode.Unauthorized);
        }
    }
}
