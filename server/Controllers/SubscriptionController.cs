using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.Domain;
using server.Payments;
using server.Services;
using server.Utils;

namespace server.Controllers;

/// <summary>
/// Endpoints para gerenciamento de assinaturas e pagamentos
/// </summary>
[ApiController]
public class SubscriptionController : AuthenticatedController
{
    private readonly Context _context;
    private readonly SubscriptionBillingService _billing;
    private readonly ILogger<SubscriptionController> _logger;
    private readonly List<string> _tags = new() { "SubscriptionController", "Controllers", "Subscription" };

    public SubscriptionController(
        ILogger<SubscriptionController> logger,
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory,
        SubscriptionBillingService billing
    ) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
        _billing = billing;
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
    /// Retorna a degustação concedida no cadastro enquanto o aviso ainda não foi confirmado.
    /// </summary>
    [HttpGet("subscription/trial-welcome")]
    [ProducesResponseType(typeof(TrialWelcomeResponse), StatusCodes.Status200OK)]
    public TrialWelcomeResponse GetTrialWelcome()
    {
        var subscription = _context.subscriptions
            .AsNoTracking()
            .Where(item =>
                item.user_id == _user.id &&
                item.status == SubscriptionStatus.Active &&
                item.payment_method == "trial" &&
                item.current_period_end > DateTime.UtcNow &&
                item.trial_welcome_pending)
            .OrderByDescending(item => item.created_at)
            .FirstOrDefault();

        if (subscription is null)
            return new TrialWelcomeResponse(false, null, null, null, null);

        var plan = Plans.GetById(subscription.plan_id);
        return new TrialWelcomeResponse(
            true,
            subscription.id,
            subscription.plan_id,
            plan?.name ?? subscription.plan_id,
            subscription.current_period_end);
    }

    /// <summary>
    /// Confirma que o usuário visualizou o aviso da degustação concedida no cadastro.
    /// </summary>
    [HttpPost("subscription/trial-welcome/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult AcknowledgeTrialWelcome()
    {
        var subscriptions = _context.subscriptions
            .Where(item =>
                item.user_id == _user.id &&
                item.payment_method == "trial" &&
                item.trial_welcome_pending)
            .ToList();

        foreach (var subscription in subscriptions)
        {
            subscription.trial_welcome_pending = false;
            subscription.updated_at = DateTime.UtcNow;
        }

        if (subscriptions.Count > 0)
            _context.SaveChanges();

        return NoContent();
    }

    /// <summary>
    /// Retorna o resumo das assinaturas do usuário.
    /// Permite exibir a assinatura ativa e a assinatura a ativar ao mesmo tempo.
    /// </summary>
    [HttpGet("subscription/overview")]
    [ProducesResponseType(typeof(SubscriptionOverviewResponse), StatusCodes.Status200OK)]
    public async Task<SubscriptionOverviewResponse> GetSubscriptionOverview(CancellationToken cancellationToken)
    {
        await RefreshPendingChargeOnOverviewAsync(cancellationToken);

        var active = await _context.subscriptions
            .Where(s => s.user_id == _user.id && s.status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.created_at)
            .FirstOrDefaultAsync(cancellationToken);

        var pending = await _context.subscriptions
            .Where(s => s.user_id == _user.id && s.status == SubscriptionStatus.PendingPayment)
            .OrderByDescending(s => s.created_at)
            .FirstOrDefaultAsync(cancellationToken);

        SubscriptionCharge? pendingCharge = null;
        try
        {
            pendingCharge = await _context.subscription_charges
                .Where(c => c.user_id == _user.id && c.status == SubscriptionChargeStatus.Pending)
                .OrderByDescending(c => c.created_at)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.UndefinedColumn ||
            ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            _logger.LogWarning(ex, "Schema de subscription_charges desatualizado. O pending_charge será omitido até a migration ser aplicada.");
        }

        return new SubscriptionOverviewResponse
        {
            active_subscription = active,
            pending_subscription = pending,
            pending_charge = pendingCharge
        };
    }

    private async Task RefreshPendingChargeOnOverviewAsync(CancellationToken cancellationToken)
    {
        try
        {
            var pendingCharge = await _context.subscription_charges
                .Where(c => c.user_id == _user.id && c.status == SubscriptionChargeStatus.Pending)
                .OrderByDescending(c => c.created_at)
                .FirstOrDefaultAsync(cancellationToken);

            if (pendingCharge == null)
                return;

            await _billing.RefreshPaymentStatusAsync(_user.id, pendingCharge.id.ToString(), cancellationToken);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.UndefinedColumn ||
            ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            _logger.LogWarning(ex, "Schema de subscription_charges desatualizado. A sincronizacao automatica do pending_charge sera ignorada ate a migration ser aplicada.");
        }
        catch (ExpectedException ex)
        {
            _logger.LogWarning(
                ex,
                "Nao foi possivel sincronizar a cobranca pendente ao carregar o overview. TraceId={TraceId} UserId={UserId} Tags={_tags_}",
                TraceContext.GetTraceId(),
                _user.id,
                _tags);
        }
    }

    /// <summary>
    /// Cria/atualiza assinatura com pagamento via cartão
    /// </summary>
    [HttpPost("subscription/card")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<PaymentResult> SubscribeWithCard([FromBody] CardSubscriptionRequest request)
    {
        var traceId = TraceContext.GetTraceId();
        var plan = Plans.GetById(request.plan_id)
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("O plano Free não requer pagamento.");

        _logger.LogInformation(
            "Tentativa de pagamento com cartão iniciada. TraceId={TraceId} UserId={UserId} Payload={@Payload} Tags={_tags_}",
            traceId,
            _user.id,
            new
            {
                request.plan_id,
                request.payment_method_id,
                request.card_type,
                request.issuer_id,
                request.installments
            }, _tags);

        await _billing.SavePayerCpfAsync(_user.id, request.payer_cpf);

        return await _billing.CreateInitialCardSubscriptionAsync(
            _user.id,
            plan,
            request.card_token,
            request.payment_method_id,
            request.card_type,
            request.issuer_id,
            request.installments);
    }


    /// <summary>
    /// Cria assinatura com pagamento via PIX
    /// </summary>
    [HttpPost("subscription/pix")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<PaymentResult> SubscribeWithPix([FromBody] PixSubscriptionRequest request)
    {
        var traceId = TraceContext.GetTraceId();
        var plan = Plans.GetById(request.plan_id)
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("O plano Free não requer pagamento.");

        _logger.LogInformation(
            "Tentativa de gerar QR Code PIX iniciada. TraceId={TraceId} UserId={UserId} Payload={@Payload} Tags={_tags_}",
            traceId,
            _user.id,
            new
            {
                request.plan_id,
                request.payer_first_name,
                request.payer_last_name
            }, _tags);

        await _billing.SavePayerCpfAsync(_user.id, request.payer_cpf);

        return await _billing.CreateInitialPixSubscriptionAsync(
            _user.id,
            plan,
            request.payer_first_name,
            request.payer_last_name);
    }


    /// <summary>
    /// Cria assinatura com pagamento via boleto
    /// </summary>
    [HttpPost("subscription/boleto")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<PaymentResult> SubscribeWithBoleto([FromBody] BoletoSubscriptionRequest request)
    {
        var traceId = TraceContext.GetTraceId();
        var plan = Plans.GetById(request.plan_id)
            ?? throw new ExpectedException("Plano inválido.");

        if (plan.price <= 0)
            throw new ExpectedException("O plano Free não requer pagamento.");

        _logger.LogInformation(
            "Tentativa de gerar boleto iniciada. TraceId={TraceId} UserId={UserId} Payload={@Payload} Tags={_tags_}",
            traceId,
            _user.id,
            new
            {
                request.plan_id,
                request.payer_first_name,
                request.payer_last_name
            }, _tags);

        await _billing.SavePayerCpfAsync(_user.id, request.payer_cpf);

        return await _billing.CreateInitialBoletoSubscriptionAsync(
            _user.id,
            plan,
            request.payer_first_name,
            request.payer_last_name);
    }


    /// <summary>
    /// Consulta status de um pagamento pendente (polling para PIX/boleto)
    /// </summary>
    [HttpGet("subscription/payment-status/{paymentId}")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    public async Task<PaymentResult> CheckPaymentStatus(string paymentId)
    {
        _logger.LogInformation(
            "Consulta de status de pagamento iniciada. TraceId={TraceId} UserId={UserId} PaymentId={PaymentId} Tags={_tags_}",
            TraceContext.GetTraceId(),
            _user.id,
            paymentId, _tags);
        return await _billing.RefreshPaymentStatusAsync(_user.id, paymentId);
    }


    /// <summary>
    /// Cancela a assinatura ativa — retorna ao plano Free
    /// </summary>
    [HttpPost("subscription/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> CancelSubscription([FromBody] CancelActiveSubscriptionRequest? request)
    {
        return CancelActiveSubscription(request);
    }

    /// <summary>
    /// Cancela a assinatura ativa.
    /// </summary>
    [HttpPost("subscription/cancel-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelActiveSubscription([FromBody] CancelActiveSubscriptionRequest? request)
    {
        request ??= new CancelActiveSubscriptionRequest();

        var mode = request.mode?.Equals("refund_prorated", StringComparison.OrdinalIgnoreCase) == true
            ? SubscriptionCancellationMode.RefundProrated
            : SubscriptionCancellationMode.EndOfPeriod;

        var result = await _billing.CancelActiveSubscriptionAsync(_user.id, request.confirm, mode);
        return Ok(result);
    }

    /// <summary>
    /// Reverte uma programação de cancelamento da assinatura ativa.
    /// </summary>
    [HttpPost("subscription/cancel-scheduled/revert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevertScheduledCancellation([FromBody] RevertScheduledCancellationRequest? request)
    {
        request ??= new RevertScheduledCancellationRequest();

        var result = await _billing.RevertScheduledCancellationAsync(_user.id, request.confirm);
        return Ok(result);
    }

    /// <summary>
    /// Cancela apenas a assinatura pendente de compensação.
    /// </summary>
    [HttpPost("subscription/cancel-pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelPendingSubscription()
    {
        await _billing.CancelPendingSubscriptionAsync(_user.id);

        return Ok(new { message = "Cobrança pendente cancelada." });
    }
}


// ======================== REQUEST OBJECTS ========================

public class CardSubscriptionRequest
{
    public string plan_id { get; set; } = string.Empty;
    public string card_token { get; set; } = string.Empty;
    public string payment_method_id { get; set; } = string.Empty;
    public string card_type { get; set; } = "credit_card";
    public string issuer_id { get; set; } = string.Empty;
    public int installments { get; set; } = 1;
    public string payer_cpf { get; set; } = string.Empty;
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

public class SubscriptionOverviewResponse
{
    public Subscription? active_subscription { get; set; }
    public Subscription? pending_subscription { get; set; }
    public SubscriptionCharge? pending_charge { get; set; }
}

public record TrialWelcomeResponse(
    bool show,
    Guid? subscription_id,
    string? plan_id,
    string? plan_name,
    DateTime? expires_at);

public class CancelActiveSubscriptionRequest
{
    public bool confirm { get; set; }
    public string? mode { get; set; }
}

public class RevertScheduledCancellationRequest
{
    public bool confirm { get; set; }
}
