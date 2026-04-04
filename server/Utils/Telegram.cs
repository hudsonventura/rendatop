using System.Text;

namespace server.Utils;

public class Telegram : INotification
{
    private readonly HttpClient _httpClient = new HttpClient();
    string _token;
    string? _defaultChatId;


    public Telegram(string token, string? chatId)
    {
        _token = token;
        _defaultChatId = chatId;
    }

    public async Task Notify(string title, string message, string? chatId = null)
    {
        var targetChatId = string.IsNullOrWhiteSpace(chatId) ? _defaultChatId : chatId;
        if (string.IsNullOrWhiteSpace(targetChatId))
            throw new Exception("Chat ID do Telegram não configurado.");

        var formattedMessage = $"<b>[{title}]</b><br><br>{message}";
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.telegram.org/bot" + _token + "/sendMessage")
        {
            Content = new StringContent(
                "{\"chat_id\":\"" + targetChatId +
                "\",\"text\":\"" + formattedMessage.Replace("<br>", "\n") + "\",\"parse_mode\":\"HTML\"}",
                Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Erro ao enviar mensagem para o Telegram: {response.StatusCode}");
        }
    }
}
