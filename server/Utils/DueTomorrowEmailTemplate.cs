using System.Net;
using server.BackgroundServices;
using server.Domain;

namespace server.Utils;

public static class DueTomorrowEmailTemplate
{
    private static readonly TimeZoneInfo BrasiliaTimeZone = ResolveBrasiliaTimeZone();

    internal static string Build(
        User user,
        Investment investment,
        DueTomorrowNotificationSummary summary,
        string? clientBaseUrl)
    {
        var bankName = investment.bank?.Name ?? "Banco nao informado";
        var due = FormatLocalDateTime(investment.due_date ?? DateTime.UtcNow);
        var investedValue = $"R$ {investment.value:N2}";
        var grossProfit = $"R$ {summary.GrossProfit:N2}";
        var incomeTax = $"R$ {summary.IncomeTax:N2}";
        var netValue = $"R$ {summary.NetValue:N2}";
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
                                      <div style="font-size:24px; line-height:32px; color:#ffffff; font-weight:700; margin-top:4px;">Vencimento amanha</div>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:32px;">
                                <p style="margin:0 0 12px; font-size:16px; line-height:24px;">Ola, {HtmlEncode(user.name)}!</p>
                                <p style="margin:0; font-size:15px; line-height:24px; color:#374151;">
                                  Identificamos que um dos seus investimentos vence amanha. Revise os detalhes abaixo para se planejar para o resgate ou para a reaplicacao.
                                </p>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:24px; border:1px solid #e5e7eb; border-radius:10px; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:20px 22px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:12px;">Resumo do vencimento</div>
                                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Investimento</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827; font-weight:600;">{HtmlEncode(investment.title)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Banco</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(bankName)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Valor investido</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827; font-weight:700;">{HtmlEncode(investedValue)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Rendimento bruto</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(grossProfit)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">IR</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#dc2626; font-weight:700;">{HtmlEncode(incomeTax)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Valor liquido</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#16a34a; font-weight:700;">{HtmlEncode(netValue)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Vencimento</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(due)} (Brasília)</td>
                                        </tr>
                                      </table>
                                    </td>
                                  </tr>
                                </table>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:20px; border-left:4px solid #0f172a; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:18px 20px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:8px;">Proximo passo</div>
                                      <div style="font-size:14px; line-height:22px; color:#1f2937;">Acesse o RendaTop para revisar seus resgates e decidir se deseja sacar, reaplicar ou apenas acompanhar a liquidacao prevista para este investimento.</div>
                                    </td>
                                  </tr>
                                </table>

                                <p style="margin:24px 0 0; font-size:12px; line-height:20px; color:#6b7280;">
                                  Este lembrete foi enviado automaticamente com base na data de vencimento cadastrada no investimento.
                                </p>
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

    private static string FormatLocalDateTime(DateTime value)
    {
        var brasiliaTime = TimeZoneInfo.ConvertTimeFromUtc(UtcDateTime.EnsureUtc(value), BrasiliaTimeZone);
        return $"{brasiliaTime:dd/MM/yyyy HH:mm}";
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
