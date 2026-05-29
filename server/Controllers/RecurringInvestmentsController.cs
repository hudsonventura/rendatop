using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.RequestObjects;
using server.Utils;

namespace server.Controllers;

[ApiController]
public class RecurringInvestmentsController : AuthenticatedController
{
    private readonly Context _context;

    public RecurringInvestmentsController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("Investments/Recurring")]
    [ProducesResponseType(typeof(RecurringInvestmentsOverviewResponse), StatusCodes.Status200OK)]
    public IActionResult Get([FromQuery] Guid? wallet_id = null)
    {
        var wallet = WalletAccess.ResolveAccessibleWallet(_context, _user, wallet_id);
        var recurringInvestments = _context.recurring_investments
            .AsNoTracking()
            .Include(item => item.bank)
            .Where(item => item.owner_id == _user.id && (item.wallet_id == wallet.id || item.wallet_id == null))
            .OrderByDescending(item => item.active)
            .ThenBy(item => item.title)
            .ToList()
            .Select(ToResponse)
            .ToList();

        return Ok(new RecurringInvestmentsOverviewResponse(
            SubscriptionFeatureAccess.CanUseRecurringInvestments(_context, _user.id),
            recurringInvestments));
    }

    [HttpPost("Investments/Recurring")]
    [ProducesResponseType(typeof(RecurringInvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] RecurringInvestmentRequest request)
    {
        EnsureRecurringInvestmentsEnabled();
        ValidateRequest(request);

        var bank = _context.banks.FirstOrDefault(item => item.Code == (ushort)request.bank_code)
            ?? throw new ExpectedException($"Banco com código {request.bank_code} não encontrado.");
        var wallet = WalletAccess.ResolveAccessibleWallet(_context, _user, request.wallet_id);

        var user = _context.users.FirstOrDefault(item => item.id == _user.id)
            ?? throw new ExpectedException("Usuário não encontrado.");

        var recurringInvestment = new RecurringInvestment(request, user, bank);
        recurringInvestment.wallet = wallet;
        recurringInvestment.wallet_id = wallet.id;
        TryGenerateImmediateInvestment(recurringInvestment);
        _context.recurring_investments.Add(recurringInvestment);
        _context.SaveChanges();

        recurringInvestment.bank = bank;
        return Ok(ToResponse(recurringInvestment));
    }

    [HttpPatch("Investments/Recurring/{id}")]
    [ProducesResponseType(typeof(RecurringInvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult Update(Guid id, [FromBody] RecurringInvestmentRequest request)
    {
        EnsureRecurringInvestmentsEnabled();
        ValidateRequest(request);

        var recurringInvestment = _context.recurring_investments
            .Include(item => item.owner)
            .Include(item => item.bank)
            .FirstOrDefault(item => item.id == id && item.owner_id == _user.id)
            ?? throw new ExpectedException("Recorrência não encontrada.");

        var bank = _context.banks.FirstOrDefault(item => item.Code == (ushort)request.bank_code)
            ?? throw new ExpectedException($"Banco com código {request.bank_code} não encontrado.");
        var wallet = WalletAccess.ResolveAccessibleWallet(_context, _user, request.wallet_id ?? recurringInvestment.wallet_id);

        recurringInvestment.Apply(request, bank);
        recurringInvestment.wallet = wallet;
        recurringInvestment.wallet_id = wallet.id;
        TryGenerateImmediateInvestment(recurringInvestment);
        _context.SaveChanges();

        recurringInvestment.bank = bank;
        return Ok(ToResponse(recurringInvestment));
    }

    [HttpPatch("Investments/Recurring/{id}/active")]
    [ProducesResponseType(typeof(RecurringInvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult UpdateActive(Guid id, [FromBody] RecurringInvestmentActiveRequest request)
    {
        var recurringInvestment = _context.recurring_investments
            .Include(item => item.owner)
            .Include(item => item.bank)
            .FirstOrDefault(item => item.id == id && item.owner_id == _user.id)
            ?? throw new ExpectedException("Recorrência não encontrada.");

        if (request.active)
            EnsureRecurringInvestmentsEnabled();

        WalletAccess.ResolveAccessibleWallet(_context, _user, recurringInvestment.wallet_id);

        recurringInvestment.active = request.active;
        recurringInvestment.updated_at = DateTime.UtcNow;
        TryGenerateImmediateInvestment(recurringInvestment);
        _context.SaveChanges();

        return Ok(ToResponse(recurringInvestment));
    }

    [HttpDelete("Investments/Recurring/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult Delete(Guid id)
    {
        var recurringInvestment = _context.recurring_investments
            .FirstOrDefault(item => item.id == id && item.owner_id == _user.id)
            ?? throw new ExpectedException("Recorrência não encontrada.");

        WalletAccess.ResolveAccessibleWallet(_context, _user, recurringInvestment.wallet_id);

        _context.recurring_investments.Remove(recurringInvestment);
        _context.SaveChanges();

        return NoContent();
    }

    private void EnsureRecurringInvestmentsEnabled()
    {
        if (!SubscriptionFeatureAccess.CanUseRecurringInvestments(_context, _user.id))
            throw new ExpectedException("Investimentos recorrentes exigem um plano pago ativo.");
    }

    private static void ValidateRequest(RecurringInvestmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.title))
            throw new ExpectedException("Título é obrigatório.");

        if (request.value <= 0)
            throw new ExpectedException("Valor do investimento deve ser maior que zero.");

        if (request.index_percent < 0)
            throw new ExpectedException("Valor do indexador não pode ser negativo.");

        if (!request.liquidity_daily && (!request.duration_days.HasValue || request.duration_days.Value <= 0))
            throw new ExpectedException("Informe a duração em dias para recorrências sem liquidez diária.");

        if (request.frequency == RecurringInvestmentFrequency.Weekly)
        {
            var weekdays = request.weekdays ?? [];
            if (weekdays.Count == 0)
                throw new ExpectedException("Selecione pelo menos um dia da semana para a recorrência.");

            if (weekdays.Any(day => day < 0 || day > 6))
                throw new ExpectedException("Os dias da semana selecionados são inválidos.");
        }
        else
        {
            if (!request.day_of_month.HasValue || request.day_of_month.Value < 1 || request.day_of_month.Value > 31)
                throw new ExpectedException("Informe um dia do mês entre 1 e 31.");

            var months = request.months ?? [];
            if (months.Count == 0)
                throw new ExpectedException("Selecione pelo menos um mês para a recorrência mensal.");

            if (months.Any(month => month < 1 || month > 12))
                throw new ExpectedException("Os meses selecionados são inválidos.");
        }
    }

    private static RecurringInvestmentResponse ToResponse(RecurringInvestment recurringInvestment)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextOccurrence = recurringInvestment.GetNextOccurrence(today);

        return new RecurringInvestmentResponse(
            recurringInvestment.id,
            recurringInvestment.wallet_id,
            recurringInvestment.title,
            recurringInvestment.investment_type,
            recurringInvestment.bank.Code,
            recurringInvestment.bank.Name,
            recurringInvestment.value,
            recurringInvestment.index,
            recurringInvestment.index_percent,
            recurringInvestment.index_value,
            recurringInvestment.taxes,
            recurringInvestment.liquidity_daily,
            recurringInvestment.duration_days,
            recurringInvestment.frequency,
            recurringInvestment.weekdays,
            recurringInvestment.day_of_month,
            recurringInvestment.GetMonths(),
            recurringInvestment.active,
            recurringInvestment.last_generated_at,
            nextOccurrence?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            recurringInvestment.created_at,
            recurringInvestment.updated_at
        );
    }

    private void TryGenerateImmediateInvestment(RecurringInvestment recurringInvestment)
    {
        if (!recurringInvestment.active)
            return;

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (!recurringInvestment.MatchesDate(today))
            return;

        if (recurringInvestment.last_generated_at.HasValue &&
            DateOnly.FromDateTime(recurringInvestment.last_generated_at.Value.ToLocalTime()) == today)
        {
            return;
        }

        var investmentRequest = recurringInvestment.ToInvestmentRequest(today);
        var investment = new Investment(investmentRequest, recurringInvestment.owner, recurringInvestment.bank);
        _context.Entry(investment.owner).State = EntityState.Unchanged;
        _context.Entry(investment.bank).State = EntityState.Unchanged;
        investment.wallet_id = recurringInvestment.wallet_id;
        investment.wallet = recurringInvestment.wallet;
        _context.investments.Add(investment);

        recurringInvestment.last_generated_at = DateTime.UtcNow;
        recurringInvestment.updated_at = DateTime.UtcNow;
    }
}

public record RecurringInvestmentsOverviewResponse(
    bool recurring_investments_enabled,
    List<RecurringInvestmentResponse> items
);

public record RecurringInvestmentActiveRequest(
    bool active
);

public record RecurringInvestmentResponse(
    Guid id,
    Guid? wallet_id,
    string title,
    InvestmentType? investment_type,
    int bank_code,
    string bank_name,
    decimal value,
    IdexesType index,
    decimal index_percent,
    decimal index_value,
    bool taxes,
    bool liquidity_daily,
    int? duration_days,
    RecurringInvestmentFrequency frequency,
    List<short> weekdays,
    int? day_of_month,
    List<int> months,
    bool active,
    DateTime? last_generated_at,
    DateTime? next_occurrence_at,
    DateTime created_at,
    DateTime updated_at
);
