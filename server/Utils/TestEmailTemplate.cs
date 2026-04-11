using System.Net;
using server.Domain;

namespace server.Utils;

public static class TestEmailTemplate
{
    private static readonly TimeZoneInfo BrasiliaTimeZone = ResolveBrasiliaTimeZone();

    public static string Build(User user, string? clientBaseUrl, DateTime? sentAtUtc = null)
    {
        var sentAt = FormatBrasiliaDateTime(sentAtUtc ?? DateTime.UtcNow);
        var iconUrl = BuildClientAssetUrl(clientBaseUrl, "icon.png");
        var iconMarkup = string.IsNullOrWhiteSpace(iconUrl)
            ? string.Empty
            : $"""
                    <td style="padding-right:16px; vertical-align:middle;">
                      <img src="{HtmlEncode(iconUrl)}" alt="RendaTop" width="40" height="40" style="display:block; width:40px; height:40px; border:0; outline:none; text-decoration:none;">
                    </td>
                """;

        return $"""
                <!DOCTYPE html>
                <html lang="pt-BR">
                  <body style="margin:0; padding:0; background-color:#f3f4f6; font-family:Arial, Helvetica, sans-serif; color:#111827;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f3f4f6; margin:0; padding:24px 12px;">
                      <tr>
                        <td align="center">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:680px; background-color:#ffffff; border:1px solid #d1d5db; border-radius:12px; overflow:hidden;">
                            <tr>
                              <td style="padding:28px 32px; background-color:#0f172a;">
                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                  <tr>
                                    {iconMarkup}
                                    <td style="vertical-align:middle;">
                                      <div style="font-size:12px; letter-spacing:0.12em; text-transform:uppercase; color:#cbd5e1; font-weight:700;">RendaTop</div>
                                      <div style="font-size:24px; line-height:32px; color:#ffffff; font-weight:700; margin-top:4px;">E-mail de teste</div>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:32px;">
                                <p style="margin:0 0 12px; font-size:16px; line-height:24px;">Ola, {HtmlEncode(user.name)}!</p>
                                <p style="margin:0; font-size:15px; line-height:24px; color:#374151;">
                                  Este e-mail confirma que o canal de notificacoes por e-mail da sua conta no RendaTop esta funcionando corretamente.
                                </p>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:24px; border:1px solid #e5e7eb; border-radius:10px; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:20px 22px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:12px;">Resumo do teste</div>
                                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Canal</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827; font-weight:600;">E-mail</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Destino</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(user.email)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Enviado em</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(sentAt)}</td>
                                        </tr>
                                      </table>
                                    </td>
                                  </tr>
                                </table>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:20px; border-left:4px solid #0f172a; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:18px 20px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:8px;">Status</div>
                                      <div style="font-size:14px; line-height:22px; color:#1f2937;">Se voce recebeu esta mensagem, o envio de notificacoes por e-mail esta operacional para a sua conta.</div>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>
                """;
    }

    private static string FormatBrasiliaDateTime(DateTime value)
    {
        var brasiliaTime = TimeZoneInfo.ConvertTimeFromUtc(UtcDateTime.EnsureUtc(value), BrasiliaTimeZone);
        return $"{brasiliaTime:dd/MM/yyyy HH:mm} (Brasília)";
    }

    private static string HtmlEncode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string? BuildClientAssetUrl(string? baseUrl, string relativePath)
    {
        var normalizedBaseUrl = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedBaseUrl))
            return null;

        if (!Uri.TryCreate(normalizedBaseUrl.EndsWith('/') ? normalizedBaseUrl : normalizedBaseUrl + "/", UriKind.Absolute, out var baseUri))
            return null;

        return new Uri(baseUri, relativePath.TrimStart('/')).ToString();
    }

    private static TimeZoneInfo ResolveBrasiliaTimeZone()
    {
        string[] timeZoneIds =
        [
            "America/Sao_Paulo",
            "E. South America Standard Time"
        ];

        foreach (var timeZoneId in timeZoneIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
