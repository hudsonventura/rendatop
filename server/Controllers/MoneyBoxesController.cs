using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using System.Net;
using server.Utils;

namespace server.Controllers;

[ApiController]
public class MoneyBoxesController : AuthenticatedController
{
    private readonly Context _context;

    public MoneyBoxesController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("MoneyBoxes")]
    [ProducesResponseType(typeof(MoneyBoxesOverviewResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var rawItems = _context.money_boxes
            .AsNoTracking()
            .Where(item => item.owner_id == _user.id)
            .OrderBy(item => item.name)
            .ToList();

        var moneyBoxIds = rawItems.Select(item => item.id).ToHashSet();
        var investments = _context.investments
            .AsNoTracking()
            .Include(item => item.bank)
            .Include(item => item.redemptions)
            .Where(item => item.owner.id == _user.id && item.money_box_id.HasValue && moneyBoxIds.Contains(item.money_box_id.Value))
            .ToList();

        var totalsByMoneyBoxId = investments
            .GroupBy(item => item.money_box_id!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(GetDisplayLiquidValue));

        var items = rawItems
            .Select(item => new MoneyBoxResponse(
                item.id,
                item.name,
                totalsByMoneyBoxId.GetValueOrDefault(item.id, 0m),
                item.created_at,
                item.updated_at))
            .ToList();

        var limit = SubscriptionFeatureAccess.GetMoneyBoxesLimit(_context, _user.id);
        var selectionEnabled = SubscriptionFeatureAccess.CanSelectMoneyBoxes(_context, _user.id, rawItems.Count);
        var canCreate = SubscriptionFeatureAccess.CanCreateMoneyBoxes(_context, _user.id, rawItems.Count);
        var plan = SubscriptionFeatureAccess.GetEffectivePlan(_context, _user.id);

        return Ok(new MoneyBoxesOverviewResponse(
            items,
            rawItems.Count,
            limit == int.MaxValue ? null : limit,
            canCreate,
            selectionEnabled,
            plan.id,
            BuildRestrictionMessage(plan.id, rawItems.Count, limit, selectionEnabled, canCreate)));
    }

    [HttpPost("MoneyBoxes")]
    [ProducesResponseType(typeof(MoneyBoxResponse), StatusCodes.Status200OK)]
    public IActionResult Create([FromBody] MoneyBoxRequest request)
    {
        var user = _context.users.FirstOrDefault(item => item.id == _user.id)
            ?? throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var normalizedName = NormalizeName(request.name);
        var existingCount = _context.money_boxes.Count(item => item.owner_id == _user.id);
        if (!SubscriptionFeatureAccess.CanCreateMoneyBoxes(_context, _user.id, existingCount))
            throw new ExpectedException("Usuários do plano Free podem criar até 3 cofrinhos. Faça upgrade do plano para liberar cofrinhos ilimitados.");

        EnsureUniqueName(normalizedName, null);

        var item = new MoneyBox
        {
            owner = user,
            owner_id = user.id,
            name = normalizedName,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
        };

        _context.money_boxes.Add(item);
        _context.SaveChanges();

        return Ok(new MoneyBoxResponse(item.id, item.name, 0m, item.created_at, item.updated_at));
    }

    [HttpPatch("MoneyBoxes/{id}")]
    [ProducesResponseType(typeof(MoneyBoxResponse), StatusCodes.Status200OK)]
    public IActionResult Update(Guid id, [FromBody] MoneyBoxRequest request)
    {
        var item = _context.money_boxes.FirstOrDefault(entry => entry.id == id && entry.owner_id == _user.id)
            ?? throw new ExpectedException("Cofrinho não encontrado.", HttpStatusCode.NotFound);

        var normalizedName = NormalizeName(request.name);
        EnsureUniqueName(normalizedName, id);

        item.name = normalizedName;
        item.updated_at = DateTime.UtcNow;
        _context.SaveChanges();

        var totalLiquidValue = _context.investments
            .AsNoTracking()
            .Include(investment => investment.bank)
            .Include(investment => investment.redemptions)
            .Where(investment => investment.owner.id == _user.id && investment.money_box_id == item.id)
            .ToList()
            .Sum(GetDisplayLiquidValue);

        return Ok(new MoneyBoxResponse(item.id, item.name, totalLiquidValue, item.created_at, item.updated_at));
    }

    [HttpDelete("MoneyBoxes/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Delete(Guid id)
    {
        var item = _context.money_boxes.FirstOrDefault(entry => entry.id == id && entry.owner_id == _user.id)
            ?? throw new ExpectedException("Cofrinho não encontrado.", HttpStatusCode.NotFound);

        var investments = _context.investments
            .Where(investment => investment.owner.id == _user.id && investment.money_box_id == id)
            .ToList();

        foreach (var investment in investments)
            investment.money_box_id = null;

        _context.money_boxes.Remove(item);
        _context.SaveChanges();

        return NoContent();
    }

    private void EnsureUniqueName(string normalizedName, Guid? currentId)
    {
        var exists = _context.money_boxes
            .AsNoTracking()
            .Any(item =>
                item.owner_id == _user.id &&
                item.id != currentId &&
                item.name.ToLower() == normalizedName.ToLower());

        if (exists)
            throw new ExpectedException("Você já possui um cofrinho com esse nome.");
    }

    private static string NormalizeName(string? name)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ExpectedException("Nome do cofrinho é obrigatório.");

        return normalizedName;
    }

    private static string? BuildRestrictionMessage(string planId, int count, int limit, bool selectionEnabled, bool canCreate)
    {
        if (planId != "free")
            return null;

        if (!selectionEnabled)
            return "Seu plano atual e Free e permite ate 3 cofrinhos. Como sua conta possui mais do que isso, a selecao de cofrinho fica bloqueada na criacao e edicao de investimentos ate voce reduzir a quantidade ou voltar para um plano pago.";

        if (!canCreate && limit != int.MaxValue)
            return $"Seu plano atual permite até {limit} cofrinhos. Exclua um cofrinho ou faca upgrade para criar novos.";

        return $"Seu plano atual permite até {limit} cofrinhos. Voce esta usando {count} de {limit}.";
    }

    private decimal GetDisplayLiquidValue(Investment investment)
    {
        var calcType = typeof(ICalculator).Assembly.GetType($"server.Domain.Calculator_{investment.index}");
        if (calcType == null)
            throw new ExpectedException($"Tipo de calculo nao encontrado: Calculator_{investment.index}");

        var calculator = (ICalculator)Activator.CreateInstance(calcType, _context)!;
        var calc = calculator.Calculate(investment.ToRequest()).FirstOrDefault();
        if (calc == null)
            return investment.value;

        var redeemedTotal = investment.redemptions?.Sum(redemption => redemption.value) ?? 0m;
        if (redeemedTotal <= 0m || calc.value_liq <= 0m)
            return calc.value_liq;

        var ratio = Math.Min(1m, redeemedTotal / calc.value_liq);
        var adjustedPrincipal = investment.value - (investment.value * ratio);
        var adjustedProfitLiq = calc.profit_liq - (calc.profit_liq * ratio);

        return Math.Max(0m, adjustedPrincipal + adjustedProfitLiq);
    }
}

public record MoneyBoxRequest(string name);

public record MoneyBoxResponse(
    Guid id,
    string name,
    decimal total_liquid_value,
    DateTime created_at,
    DateTime updated_at
);

public record MoneyBoxesOverviewResponse(
    List<MoneyBoxResponse> items,
    int count,
    int? limit,
    bool can_create,
    bool selection_enabled,
    string active_plan_id,
    string? restriction_message
);
