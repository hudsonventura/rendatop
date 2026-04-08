using System.Net.Http.Json;
using System.Text.Json;

namespace server.Utils;

/// <summary>
/// Envia notificações via WWebJS REST API.
/// </summary>
public class WWebJsWhatsAppNotification : IWhatsAppNotification
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly string _sessionId;
    private bool _sessionInitialized;

    public WWebJsWhatsAppNotification(string? baseUrl, string? apiKey, string? sessionId)
    {
        _baseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        _apiKey = (apiKey ?? string.Empty).Trim();
        _sessionId = string.IsNullOrWhiteSpace(sessionId) ? "Default" : sessionId.Trim();
    }

    public async Task Notify(string phone, string title, string message)
    {
        EnsureConfigured();
        await EnsureSessionAsync();

        var destination = ToChatId(phone);
        var text = $"[{title}] {Environment.NewLine}{message}".Trim();

        var response = await SendAsync(HttpMethod.Post, $"/client/sendMessage/{_sessionId}", new
        {
            chatId = destination,
            contentType = "string",
            content = text
        });

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();

        if (body.Contains("session_not_found", StringComparison.OrdinalIgnoreCase))
        {
            _sessionInitialized = false;
            await EnsureSessionAsync();

            response = await SendAsync(HttpMethod.Post, $"/client/sendMessage/{_sessionId}", new
            {
                chatId = destination,
                contentType = "string",
                content = text
            });

            if (response.IsSuccessStatusCode)
                return;

            body = await response.Content.ReadAsStringAsync();
        }

        throw new Exception($"Erro ao enviar mensagem para o WhatsApp via WWebJS: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }

    private async Task EnsureSessionAsync()
    {
        if (_sessionInitialized)
            return;

        await _sessionLock.WaitAsync();
        try
        {
            if (_sessionInitialized)
                return;

            var statusResponse = await SendAsync(HttpMethod.Get, $"/session/status/{_sessionId}");
            var statusBody = await statusResponse.Content.ReadAsStringAsync();

            if (statusResponse.IsSuccessStatusCode && !ContainsSessionNotFound(statusBody))
            {
                _sessionInitialized = true;
                return;
            }

            var startResponse = await SendAsync(HttpMethod.Get, $"/session/start/{_sessionId}");
            var startBody = await startResponse.Content.ReadAsStringAsync();

            if (!startResponse.IsSuccessStatusCode)
                throw new Exception($"Falha ao iniciar sessão WWebJS '{_sessionId}': {(int)startResponse.StatusCode} {startResponse.ReasonPhrase}. {startBody}");

            _sessionInitialized = true;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");

        if (!string.IsNullOrWhiteSpace(_apiKey))
            request.Headers.Add("x-api-key", _apiKey);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return await _httpClient.SendAsync(request);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
            throw new Exception("Configuração do WhatsApp via WWebJS incompleta. Defina WHATSAPP_WWEBJS_URL.");
    }

    private static bool ContainsSessionNotFound(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        if (body.Contains("session_not_found", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var messageProperty))
                return string.Equals(messageProperty.GetString(), "session_not_found", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
        }

        return false;
    }

    private static string ToChatId(string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length == 11)
            return $"{NormalizeBrazilMobileWithNinthDigit(digits)}@c.us";

        if ((digits.Length == 12 || digits.Length == 13) && digits.StartsWith("55"))
            return $"{NormalizeBrazilMobileWithNinthDigit(digits)}@c.us";

        throw new Exception("Telefone inválido para WhatsApp. Use 11 dígitos no formato 99999999999 ou informe com DDI.");
    }

    /// <summary>
    /// O wwebjs pode exigir o JID sem o nono dígito em alguns celulares BR.
    /// Ex.: 65992327494 -> 556592327494
    /// </summary>
    internal static string NormalizeBrazilMobileWithNinthDigit(string digits)
    {
        if (digits.Length == 11)
            return $"55{digits[..2]}{digits[3..]}";

        if (digits.Length == 13 && digits.StartsWith("55"))
            return $"55{digits[2..4]}{digits[5..]}";

        return digits;
    }
}
