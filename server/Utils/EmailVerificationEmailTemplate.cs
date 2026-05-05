using System.Net;
using server.Domain;

namespace server.Utils;

public static class EmailVerificationEmailTemplate
{
    public static string BuildSignup(User user, string code, string? clientBaseUrl)
        => Build(
            user,
            code,
            clientBaseUrl,
            badge: "Ativacao de conta",
            title: "Verificacao de email",
            description: "Use o codigo abaixo para confirmar seu cadastro e ativar sua conta na plataforma RendaTop.",
            summaryLabel: "Resumo da verificacao",
            targetLabel: "Email do cadastro",
            targetValue: user.email,
            codeLabel: "Codigo de verificacao",
            instructionTitle: "Proximo passo",
            instructionText: "Informe este codigo na tela de cadastro para concluir a ativacao da sua conta.",
            footerText: "Se voce nao solicitou este cadastro, ignore este e-mail.");

    public static string BuildEmailChange(User user, string pendingEmail, string code, string? clientBaseUrl)
        => Build(
            user,
            code,
            clientBaseUrl,
            badge: "Alteracao de email",
            title: "Verificacao de novo email",
            description: "Recebemos uma solicitacao para alterar o email principal da sua conta na plataforma RendaTop.",
            summaryLabel: "Resumo da alteracao",
            targetLabel: "Novo email",
            targetValue: pendingEmail,
            codeLabel: "Codigo de verificacao",
            instructionTitle: "Proximo passo",
            instructionText: "Informe este codigo na tela de configuracoes para confirmar a troca do email da sua conta.",
            footerText: "Se voce nao solicitou essa alteracao, ignore este e-mail.");

    private static string Build(
        User user,
        string code,
        string? clientBaseUrl,
        string badge,
        string title,
        string description,
        string summaryLabel,
        string targetLabel,
        string targetValue,
        string codeLabel,
        string instructionTitle,
        string instructionText,
        string footerText)
    {
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
                                      <div style="font-size:24px; line-height:32px; color:#ffffff; font-weight:700; margin-top:4px;">{HtmlEncode(title)}</div>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:32px;">
                                <p style="margin:0 0 12px; font-size:16px; line-height:24px;">Ola, {HtmlEncode(user.name)}!</p>
                                <p style="margin:0; font-size:15px; line-height:24px; color:#374151;">
                                  {HtmlEncode(description)}
                                </p>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:24px; border:1px solid #e5e7eb; border-radius:10px; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:20px 22px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:12px;">{HtmlEncode(summaryLabel)}</div>
                                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Tipo</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827; font-weight:600;">{HtmlEncode(badge)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">{HtmlEncode(targetLabel)}</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(targetValue)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">{HtmlEncode(codeLabel)}</td>
                                          <td align="right" style="padding:8px 0; font-size:20px; letter-spacing:0.16em; color:#111827; font-weight:700;">{HtmlEncode(code)}</td>
                                        </tr>
                                      </table>
                                    </td>
                                  </tr>
                                </table>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:20px; border-left:4px solid #0f172a; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:18px 20px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:8px;">{HtmlEncode(instructionTitle)}</div>
                                      <div style="font-size:14px; line-height:22px; color:#1f2937;">{HtmlEncode(instructionText)}</div>
                                    </td>
                                  </tr>
                                </table>

                                <p style="margin:24px 0 0; font-size:12px; line-height:20px; color:#6b7280;">
                                  {HtmlEncode(footerText)}
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
}
