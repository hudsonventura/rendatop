using System.Text;

namespace server.Utils;

/// <summary>
/// Envia notificações via Evolution API (open source, self-hosted)
/// </summary>
public class WhatsApp : IWhatsAppNotification
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly string _baseUrl;
    private readonly string _instance;
    private readonly string _apiKey;

    public WhatsApp(string? baseUrl, string? instance, string? apiKey)
    {
        _baseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        _instance = (instance ?? string.Empty).Trim();
        _apiKey = (apiKey ?? string.Empty).Trim();
    }

    public async Task Notify(string phone, string title, string message)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_instance) || string.IsNullOrWhiteSpace(_apiKey))
            throw new Exception("Configuração do WhatsApp incompleta. Defina WHATSAPP_EVOLUTION_URL, WHATSAPP_EVOLUTION_INSTANCE e WHATSAPP_EVOLUTION_API_KEY.");

        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
            throw new Exception("Telefone inválido para WhatsApp. Use 11 dígitos no formato 99999999999.");

        // Converte para padrão com DDI BR para envio no Evolution API
        var destination = $"55{digits}";
        var text = $"[{title}] {Environment.NewLine}{message}".Trim();
        var json = $"{{\"number\":\"{destination}\",\"text\":\"{EscapeJson(text)}\"}}";

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/message/sendText/{_instance}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("apikey", _apiKey);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao enviar mensagem para o WhatsApp: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }
    }

    private static string EscapeJson(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
}
