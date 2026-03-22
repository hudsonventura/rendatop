using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Payments;
using server.Utils;

namespace server.Controllers;

/// <summary>
/// Endpoints para gerenciamento de assinaturas e pagamentos
/// </summary>
[ApiController]
public class SubscriptionController : AuthenticatedController
{
    private readonly Context _context;
    private readonly IPaymentProvider _payment;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ILogger<SubscriptionController> logger,
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory,
        IPaymentProvider payment
    ) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
        _payment = payment;
        _logger = logger;
    }


    /// <summary>
    /// Lista todos os planos disponíveis
    /// </summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<Plan>), StatusCodes.Status200OK)]
    public List<Plan> GetPlans() => Plans.All;


    /// <summary>
    /// Retorna a assinatura ativa do usuário
    /// </summary>
    [HttpGet("subscription")]
    [ProducesResponseType(typeof(Subscription), StatusCodes.Status200OK)]
    public Subscription? GetSubscription()
    {
        return _context.subscriptions
            .Where(s => s.user_id == _user.id)
            .OrderBy(s => s.status == SubscriptionStatus.Active ? 0
                : s.status == SubscriptionStatus.PendingPayment ? 1
                : s.status == SubscriptionStatus.Cancelled ? 2
                : 3)
            .ThenByDescending(s => s.created_at)
            .FirstOrDefault();
    }


    /// <summary>
    /// Cria/atualiza assinatura com pagamento via cartão
    /// </summary>
    [HttpPost("subscription/card")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<PaymentResult> SubscribeWithCard([FromBody] CardSubscriptionRequest request)
    {
        var plan = Plans.GetById(request.plan_id)
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("O plano Free não requer pagamento.");

        // Proteção contra cobrança dupla: verifica se já existe pagamento ativo no período
        EnsureNoDuplicateCharge(plan.id);

        var externalRef = $"sub_{_user.id}_{plan.id}_{DateTime.UtcNow:yyyyMMdd}";

        var result = await _payment.CreateCardPaymentAsync(new CardPaymentRequest
        {
            card_token = request.card_token,
            payment_method_id = request.payment_method_id,
            issuer_id = request.issuer_id,
            installments = request.installments,
            amount = plan.price,
            description = $"RendaTop - Plano {plan.name}",
            payer_email = _user.email,
            external_reference = externalRef
        });

        if (result.status == "approved")
        {
            // Salvar cartão para renovação automática
            string? customerId = null;
            string? cardId = null;
            try
            {
                var saved = await _payment.SaveCardAsync(request.card_token, _user.email);
                customerId = saved.customerId;
                cardId = saved.cardId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível salvar cartão para renovação automática");
            }

            ActivateSubscription(plan.id, request.payment_method_id.Contains("debit") ? "debit_card" : "credit_card",
                result.payment_id, customerId, cardId);
        }

        return result;
    }


    /// <summary>
    /// Cria assinatura com pagamento via PIX
    /// </summary>
    [HttpPost("subscription/pix")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<PaymentResult> SubscribeWithPix([FromBody] PixSubscriptionRequest request)
    {
        var plan = Plans.GetById(request.plan_id)
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("O plano Free não requer pagamento.");

        EnsureNoDuplicateCharge(plan.id);

        var externalRef = $"sub_{_user.id}_{plan.id}_{DateTime.UtcNow:yyyyMMdd}";

        var result = await _payment.CreatePixPaymentAsync(new PixPaymentRequest
        {
            amount = plan.price,
            description = $"RendaTop - Plano {plan.name}",
            payer_email = _user.email,
            payer_first_name = request.payer_first_name,
            payer_last_name = request.payer_last_name,
            payer_cpf = request.payer_cpf,
            external_reference = externalRef
        });

        if (result.status == "approved")
        {
            ActivateSubscription(plan.id, "pix", result.payment_id, null, null);
        }
        else
        {
            // PIX pendente — cria subscription com status PendingPayment
            CreatePendingSubscription(plan.id, "pix", result.payment_id);
        }

        return result;
    }


    /// <summary>
    /// Cria assinatura com pagamento via boleto
    /// </summary>
    [HttpPost("subscription/boleto")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<PaymentResult> SubscribeWithBoleto([FromBody] BoletoSubscriptionRequest request)
    {
        var plan = Plans.GetById(request.plan_id)
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("O plano Free não requer pagamento.");

        EnsureNoDuplicateCharge(plan.id);

        var externalRef = $"sub_{_user.id}_{plan.id}_{DateTime.UtcNow:yyyyMMdd}";

        var result = await _payment.CreateBoletoPaymentAsync(new BoletoPaymentRequest
        {
            amount = plan.price,
            description = $"RendaTop - Plano {plan.name}",
            payer_email = _user.email,
            payer_first_name = request.payer_first_name,
            payer_last_name = request.payer_last_name,
            payer_cpf = request.payer_cpf,
            external_reference = externalRef
        });

        CreatePendingSubscription(plan.id, "boleto", result.payment_id);

        return result;
    }


    /// <summary>
    /// Consulta status de um pagamento pendente (polling para PIX/boleto)
    /// </summary>
    [HttpGet("subscription/payment-status/{paymentId}")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    public async Task<PaymentResult> CheckPaymentStatus(string paymentId)
    {
        var result = await _payment.GetPaymentStatusAsync(paymentId);

        // Se pagamento agora aprovado, ativar subscription pendente
        if (result.status == "approved")
        {
            var pending = _context.subscriptions
                .Where(s => s.user_id == _user.id && s.mp_payment_id == paymentId
                            && s.status == SubscriptionStatus.PendingPayment)
                .FirstOrDefault();

            if (pending != null)
            {
                pending.status = SubscriptionStatus.Active;
                pending.current_period_start = DateTime.UtcNow;
                pending.current_period_end = DateTime.UtcNow.AddMonths(1);
                pending.updated_at = DateTime.UtcNow;

                // Cancelar outras assinaturas ativas
                CancelOtherSubscriptions(pending.id);
                _context.SaveChanges();
            }
        }

        return result;
    }


    /// <summary>
    /// Cancela a assinatura ativa — retorna ao plano Free
    /// </summary>
    [HttpPost("subscription/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult CancelSubscription()
    {
        var sub = _context.subscriptions
            .Where(s => s.user_id == _user.id
                        && s.plan_id != "free"
                        && (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment))
            .OrderBy(s => s.status == SubscriptionStatus.Active ? 0 : 1)
            .ThenByDescending(s => s.created_at)
            .FirstOrDefault();

        if (sub == null)
            throw new ExpectedException("Nenhuma assinatura ativa para cancelar.");

        var wasPending = sub.status == SubscriptionStatus.PendingPayment;
        sub.status = SubscriptionStatus.Cancelled;
        sub.updated_at = DateTime.UtcNow;
        _context.SaveChanges();

        return Ok(new
        {
            message = wasPending
                ? "Cobrança pendente cancelada."
                : "Assinatura cancelada. Você voltou ao plano Free."
        });
    }


    // ======================== HELPERS ========================

    /// <summary>
    /// LEI: Garante que não exista cobrança duplicada no mesmo mês para o mesmo plano.
    /// Verifica se já existe um pagamento approved/pending para este período.
    /// </summary>
    private void EnsureNoDuplicateCharge(string planId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        bool alreadyCharged = _context.subscriptions.Any(s =>
            s.user_id == _user.id
            && s.plan_id == planId
            && (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment)
            && s.current_period_start >= startOfMonth
            && s.current_period_start < endOfMonth
        );

        if (alreadyCharged)
        {
            throw new ExpectedException("Você já possui uma assinatura ativa ou pagamento pendente para este plano neste mês. Nenhuma cobrança adicional será realizada.");
        }
    }

    private void ActivateSubscription(string planId, string paymentMethod, string paymentId, string? customerId, string? cardId)
    {
        // Cancelar assinaturas anteriores
        var oldSubs = _context.subscriptions
            .Where(s => s.user_id == _user.id && (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment))
            .ToList();
        foreach (var old in oldSubs)
        {
            old.status = SubscriptionStatus.Cancelled;
            old.updated_at = DateTime.UtcNow;
        }

        var sub = new Subscription
        {
            user_id = _user.id,
            plan_id = planId,
            status = SubscriptionStatus.Active,
            payment_method = paymentMethod,
            mp_payment_id = paymentId,
            mp_customer_id = customerId,
            mp_card_id = cardId,
            current_period_start = DateTime.UtcNow,
            current_period_end = DateTime.UtcNow.AddMonths(1),
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        _context.subscriptions.Add(sub);
        _context.SaveChanges();
    }

    private void CreatePendingSubscription(string planId, string paymentMethod, string paymentId)
    {
        var sub = new Subscription
        {
            user_id = _user.id,
            plan_id = planId,
            status = SubscriptionStatus.PendingPayment,
            payment_method = paymentMethod,
            mp_payment_id = paymentId,
            current_period_start = DateTime.UtcNow,
            current_period_end = DateTime.UtcNow.AddMonths(1),
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        _context.subscriptions.Add(sub);
        _context.SaveChanges();
    }

    private void CancelOtherSubscriptions(Guid keepId)
    {
        var others = _context.subscriptions
            .Where(s => s.user_id == _user.id && s.id != keepId
                        && (s.status == SubscriptionStatus.Active || s.status == SubscriptionStatus.PendingPayment))
            .ToList();

        foreach (var s in others)
        {
            s.status = SubscriptionStatus.Cancelled;
            s.updated_at = DateTime.UtcNow;
        }
    }
}


// ======================== REQUEST OBJECTS ========================

public class CardSubscriptionRequest
{
    public string plan_id { get; set; } = string.Empty;
    public string card_token { get; set; } = string.Empty;
    public string payment_method_id { get; set; } = string.Empty;
    public string issuer_id { get; set; } = string.Empty;
    public int installments { get; set; } = 1;
}

public class PixSubscriptionRequest
{
    public string plan_id { get; set; } = string.Empty;
    public string payer_first_name { get; set; } = string.Empty;
    public string payer_last_name { get; set; } = string.Empty;
    public string payer_cpf { get; set; } = string.Empty;
}

public class BoletoSubscriptionRequest
{
    public string plan_id { get; set; } = string.Empty;
    public string payer_first_name { get; set; } = string.Empty;
    public string payer_last_name { get; set; } = string.Empty;
    public string payer_cpf { get; set; } = string.Empty;
}
