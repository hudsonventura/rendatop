namespace server.Utils;

/// <summary>
/// Seleciona o provider principal do WhatsApp e usa fallback quando configurado.
/// </summary>
public class FallbackWhatsAppNotification : IWhatsAppNotification
{
    private readonly string _provider;
    private readonly string? _fallbackProvider;
    private readonly IWhatsAppNotification _wwebjs;
    private readonly IWhatsAppNotification _evolution;

    public FallbackWhatsAppNotification(
        string? provider,
        string? fallbackProvider,
        IWhatsAppNotification wwebjs,
        IWhatsAppNotification evolution)
    {
        _provider = NormalizeProvider(provider) ?? "evolution";
        _fallbackProvider = NormalizeProvider(fallbackProvider);
        _wwebjs = wwebjs;
        _evolution = evolution;
    }

    public async Task Notify(string phone, string title, string message)
    {
        var primary = Resolve(_provider);

        try
        {
            await primary.Notify(phone, title, message);
        }
        catch (Exception primaryException)
        {
            if (string.IsNullOrWhiteSpace(_fallbackProvider) || _fallbackProvider == _provider)
                throw;

            Console.WriteLine($"Provider principal do WhatsApp falhou ({_provider}). Tentando fallback '{_fallbackProvider}'. Erro: {primaryException.Message}");

            try
            {
                await Resolve(_fallbackProvider).Notify(phone, title, message);
            }
            catch (Exception fallbackException)
            {
                throw new AggregateException(
                    $"Falha ao enviar WhatsApp com provider principal '{_provider}' e fallback '{_fallbackProvider}'.",
                    primaryException,
                    fallbackException);
            }
        }
    }

    private IWhatsAppNotification Resolve(string? provider) =>
        provider switch
        {
            "wwebjs" => _wwebjs,
            "evolution" => _evolution,
            _ => throw new Exception("Provider de WhatsApp inválido. Use 'wwebjs' ou 'evolution'.")
        };

    private static string? NormalizeProvider(string? provider)
    {
        var normalized = (provider ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
