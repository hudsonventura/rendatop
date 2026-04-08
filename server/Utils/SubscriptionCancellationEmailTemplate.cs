using System.Net;
using server.Domain;
using server.Services;

namespace server.Utils;

public static class SubscriptionCancellationEmailTemplate
{
    public static string Build(
        User user,
        Subscription subscription,
        SubscriptionCancellationResult result,
        string? clientBaseUrl)
    {
        var plan = Plans.GetById(subscription.plan_id);
        var paymentMethodLabel = GetPaymentMethodLabel(subscription.payment_method);
        var requestedAt = FormatLocalDateTime(subscription.cancellation_requested_at ?? DateTime.UtcNow);
        var periodEnd = FormatLocalDateTime(subscription.current_period_end);
        var effectiveAt = FormatLocalDateTime(result.effective_at ?? subscription.current_period_end);
        var iconUrl = BuildClientAssetUrl(clientBaseUrl, "icon.png");
        var iconMarkup = string.IsNullOrWhiteSpace(iconUrl)
            ? string.Empty
            : $"""
                    <td style="padding-right:16px; vertical-align:middle;">
                      <img src="{HtmlEncode(iconUrl)}" alt="RendaTop" width="40" height="40" style="display:block; width:40px; height:40px; border:0; outline:none; text-decoration:none;">
                    </td>
                """;

        var summaryTitle = result.scheduled
            ? "Cancelamento programado"
            : "Assinatura cancelada";
        var statusDescription = result.scheduled
            ? "Sua assinatura permanecera ativa ate o fim do periodo atual. Nenhuma nova cobranca sera enviada."
            : "O cancelamento foi concluido e sua assinatura nao sera renovada automaticamente.";
        var refundLabel = result.refunded_amount.HasValue && result.refunded_amount.Value > 0
            ? $"R$ {result.refunded_amount.Value:N2}"
            : "Sem estorno";
        var refundGuidance = result.refunded_amount.HasValue && result.refunded_amount.Value > 0
            ? $"O estorno proporcional de R$ {result.refunded_amount.Value:N2} foi solicitado. O prazo de visualizacao depende da operadora do cartao, banco ou instituicao financeira responsavel pelo pagamento original. Se o pgamento foi feito via cartão de crédito, pode levar até cinco dias úteis."
            : result.scheduled
                ? "Nao havera estorno imediato. O acesso segue ativo atá o fim do periodo vigente informado abaixo."
                : "Nao havia saldo proporcional disponivel para estorno no momento do cancelamento.";
        var nextStep = result.scheduled
            ? "A assinatura sera encerrada automaticamente no fim do periodo atual. Ate la, todos os recursos do plano permanecem disponiveis."
            : "Se desejar voltar ao plano pago no futuro, sera necessario realizar uma nova contratacao da assinatura.";

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
                                      <div style="font-size:24px; line-height:32px; color:#ffffff; font-weight:700; margin-top:4px;">Cancelamento de assinatura</div>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:32px;">
                                <p style="margin:0 0 12px; font-size:16px; line-height:24px;">Ola, {HtmlEncode(user.name)}!</p>
                                <p style="margin:0; font-size:15px; line-height:24px; color:#374151;">
                                  Este e-mail confirma o processamento da sua solicitacao de cancelamento da assinatura no RendaTop.
                                </p>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:24px; border:1px solid #e5e7eb; border-radius:10px; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:20px 22px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:12px;">Resumo do cancelamento</div>
                                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Status</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827; font-weight:700;">{HtmlEncode(summaryTitle)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Plano</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827; font-weight:600;">{HtmlEncode(plan?.name ?? subscription.plan_id)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Metodo de pagamento</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(paymentMethodLabel)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Solicitado em</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(requestedAt)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Efetivado em</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(effectiveAt)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Fim do periodo atual</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(periodEnd)}</td>
                                        </tr>
                                        <tr>
                                          <td style="padding:8px 0; font-size:13px; color:#64748b;">Estorno</td>
                                          <td align="right" style="padding:8px 0; font-size:14px; color:#111827;">{HtmlEncode(refundLabel)}</td>
                                        </tr>
                                      </table>
                                    </td>
                                  </tr>
                                </table>

                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:20px; border-left:4px solid #0f172a; background-color:#f8fafc;">
                                  <tr>
                                    <td style="padding:18px 20px;">
                                      <div style="font-size:12px; letter-spacing:0.08em; text-transform:uppercase; color:#64748b; font-weight:700; margin-bottom:8px;">Prazo e acesso</div>
                                      <div style="font-size:14px; line-height:22px; color:#1f2937;">{HtmlEncode(statusDescription)}</div>
                                      <div style="margin-top:10px; font-size:14px; line-height:22px; color:#1f2937;">{HtmlEncode(refundGuidance)}</div>
                                      <div style="margin-top:10px; font-size:14px; line-height:22px; color:#1f2937;">{HtmlEncode(nextStep)}</div>
                                    </td>
                                  </tr>
                                </table>

                                <p style="margin:24px 0 0; font-size:12px; line-height:20px; color:#6b7280;">
                                  Se voce tiver qualquer divergencia nas informacoes do cancelamento ou do estorno, responda este e-mail para que possamos analisar o caso.
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
        return value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    private static string GetPaymentMethodLabel(string paymentMethod)
    {
        return paymentMethod switch
        {
            "credit_card" => "Cartao de credito",
            "debit_card" => "Cartao de debito",
            "pix" => "PIX",
            "boleto" => "Boleto",
            _ => paymentMethod
        };
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
