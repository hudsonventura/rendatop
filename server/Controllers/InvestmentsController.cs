using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.RequestObjects;
using server.Utils;
using System.Net;

namespace server.Controllers;

/// <summary>
/// Conjunto de endpoints de controle de investimentos
/// </summary>
[ApiController]
public class InvestmentsController : AuthenticatedController
{
    private const int EarlyMaturityWindowDays = 5;
    Context _context;
    ILogger<InvestmentsController> _logger;
    private readonly List<string> _tags = new() { "InvestmentsController", "Controllers", "Investment" };
    INotification _notify;

    public InvestmentsController(ILogger<InvestmentsController> logger, IHttpContextAccessor httpContextAccessor, IDbContextFactory<Context> contextFactory, INotification notify) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
        _logger = logger;
        _notify = notify;
    }

    private Investment GetInvestmentByID(Guid id)
    {
        return _context.investments
                            .Include(x => x.bank)
                            .Include(x => x.wallet)
                            .Include(x => x.money_box)
                            .Where(x => x.owner.id == _user.id && x.id == id)
                            .FirstOrDefault();
    }

    private MoneyBox? ResolveMoneyBox(Guid? moneyBoxId)
    {
        if (!moneyBoxId.HasValue)
            return null;

        return _context.money_boxes
            .FirstOrDefault(item => item.id == moneyBoxId.Value && item.owner_id == _user.id)
            ?? throw new ExpectedException("Cofrinho não encontrado.");
    }

    private void EnsureMoneyBoxSelectionAllowed(Guid? requestedMoneyBoxId, Guid? currentMoneyBoxId = null)
    {
        if (!requestedMoneyBoxId.HasValue)
            return;

        var moneyBoxesCount = _context.money_boxes.Count(item => item.owner_id == _user.id);
        var selectionEnabled = SubscriptionFeatureAccess.CanSelectMoneyBoxes(_context, _user.id, moneyBoxesCount);

        if (!selectionEnabled && requestedMoneyBoxId != currentMoneyBoxId)
            throw new ExpectedException("Seu plano Free permite apenas 3 cofrinhos ativos para selecao. Remova cofrinhos excedentes ou volte para um plano pago para escolher um cofrinho nos investimentos.");
    }

    private InvestmentLimitOverviewResponse BuildInvestmentLimitOverview()
    {
        var plan = SubscriptionFeatureAccess.GetEffectivePlan(_context, _user.id);
        var count = SubscriptionFeatureAccess.GetActiveInvestmentsCount(_context, _user.id);
        var limit = plan.investments;
        var canCreate = limit == int.MaxValue || count < limit;
        var isOverLimit = limit != int.MaxValue && count > limit;

        return new InvestmentLimitOverviewResponse(
            count,
            limit == int.MaxValue ? null : limit,
            canCreate,
            isOverLimit,
            plan.id,
            plan.name,
            BuildInvestmentLimitRestrictionMessage(plan, count, limit, canCreate, isOverLimit));
    }

    private Investment? GetValidReplacementSource(Guid? replacementSourceInvestmentId)
    {
        if (!replacementSourceInvestmentId.HasValue)
            return null;

        var source = _context.investments.FirstOrDefault(investment =>
            investment.id == replacementSourceInvestmentId.Value &&
            investment.owner.id == _user.id &&
            !investment.archived);

        if (source is null)
            return null;

        if (!CanArchiveOrReinvestBeforeDueDate(source.due_date))
            return null;

        return source;
    }

    private static bool CanArchiveOrReinvestBeforeDueDate(DateTime? dueDate)
    {
        if (!dueDate.HasValue)
            return false;

        return dueDate.Value.Date <= DateTime.UtcNow.Date.AddDays(EarlyMaturityWindowDays);
    }

    private static string? BuildInvestmentLimitRestrictionMessage(Plan plan, int count, int limit, bool canCreate, bool isOverLimit)
    {
        if (limit == int.MaxValue)
            return null;

        var limitDescription = limit == 1 ? "1 investimento ativo" : $"até {limit} investimentos ativos";

        if (isOverLimit)
            return $"Seu plano {plan.name} permite {limitDescription}. Você possui {count} investimentos ativos; pode continuar usando o sistema normalmente, mas faça upgrade para liberar novos investimentos.";

        if (!canCreate)
            return $"Seu plano {plan.name} permite {limitDescription}. Faça upgrade para adicionar novos investimentos.";

        return $"Seu plano {plan.name} permite {limitDescription}. Você está usando {count} de {limit}.";
    }

    private static List<Calculated> BuildTableCalculated(Investment investment)
    {
        if (investment.archived)
        {
            return (investment.calculated ?? new List<Calculated>())
                .Select(CloneCalculated)
                .ToList();
        }

        var redeemedTotal = investment.redemptions?.Sum(r => r.value) ?? 0m;
        var baseCalculated = investment.calculated ?? new List<Calculated>();

        if (redeemedTotal <= 0m)
        {
            return baseCalculated
                .Select(CloneCalculated)
                .ToList();
        }

        return baseCalculated
            .Select(calc => ApplyRedemptionDisplay(calc, investment.value, redeemedTotal))
            .ToList();
    }

    private static decimal GetDisplayRedemptionRatio(decimal redeemedTotal, decimal valueLiq)
    {
        if (redeemedTotal <= 0m || valueLiq <= 0m)
            return 0m;

        return Math.Min(1m, redeemedTotal / valueLiq);
    }

    private static Calculated ApplyRedemptionDisplay(Calculated calc, decimal principal, decimal redeemedTotal)
    {
        if (calc.value_liq <= 0m)
            return CloneCalculated(calc);

        var ratio = GetDisplayRedemptionRatio(redeemedTotal, calc.value_liq);

        var adjustedPrincipal = principal - (principal * ratio);
        var adjustedIofValue = calc.IOF_value - (calc.IOF_value * ratio);
        var adjustedIrValue = calc.IR_value - (calc.IR_value * ratio);
        var adjustedProfitLiq = calc.profit_liq - (calc.profit_liq * ratio);
        var adjustedProfitBrute = calc.profit_brute - (calc.profit_brute * ratio);
        var adjustedValueBrute = calc.value_brute - (calc.value_brute * ratio);

        return new Calculated
        {
            effective_index_percent_brute = calc.effective_index_percent_brute,
            profit_brute = Math.Max(0m, adjustedProfitBrute),
            value_brute = Math.Max(0m, adjustedValueBrute),
            IOF = calc.IOF,
            IOF_value = Math.Max(0m, adjustedIofValue),
            IR = calc.IR,
            IR_value = Math.Max(0m, adjustedIrValue),
            profit_liq = Math.Max(0m, adjustedProfitLiq),
            value_liq = Math.Max(0m, adjustedPrincipal + adjustedProfitLiq)
        };
    }

    private static Calculated CloneCalculated(Calculated calc)
    {
        return new Calculated
        {
            effective_index_percent_brute = calc.effective_index_percent_brute,
            profit_brute = calc.profit_brute,
            value_brute = calc.value_brute,
            IOF = calc.IOF,
            IOF_value = calc.IOF_value,
            IR = calc.IR,
            IR_value = calc.IR_value,
            profit_liq = calc.profit_liq,
            value_liq = calc.value_liq
        };
    }

    private void ArchiveIfFullyRedeemed(Investment investment)
    {
        if (investment == null)
            return;

        var calcType = typeof(ICalculator).Assembly.GetType(
            $"server.Domain.Calculator_{investment.index}"
        );

        if (calcType == null)
            throw new ExpectedException($"Tipo de calculo nao encontrado: Calculator_{investment.index}");

        var calculator = (ICalculator)Activator.CreateInstance(calcType, _context)!;
        var currentCalculated = calculator.Calculate(investment.ToRequest()).FirstOrDefault();

        if (currentCalculated == null)
            return;

        var redeemedTotal = _context.redemptions
            .Where(r => r.investment.id == investment.id)
            .Sum(r => (decimal?)r.value) ?? 0m;

        if (currentCalculated.value_liq - redeemedTotal <= 0m)
        {
            investment.archived = true;
            _context.investments.Update(investment);
        }
    }

    /// <summary>
	/// Lista todos os investimentos do usuário
	/// </summary>
	/// <returns>Lista todos os investimentos do usuário</returns>
	[ProducesResponseType(typeof(List<Error>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet("Investments")]
    public List<Investment> Get([FromQuery] Guid? wallet_id = null)
    {
        var wallet = WalletAccess.ResolveAccessibleWallet(_context, _user, wallet_id);
        List<Investment> investments = _context.investments
                                                .AsNoTracking()
                                                .Include(x => x.owner)
                                                .Include(x => x.bank)
                                                .Include(x => x.wallet)
                                                .Include(x => x.money_box)
                                                .Include(x => x.redemptions)
                                                .Where(x => x.owner.id == _user.id && (x.wallet_id == wallet.id || x.wallet_id == null))
                                                .ToList();
        foreach (var invest in investments)
        {
            var calcType = typeof(ICalculator).Assembly.GetType(
                $"server.Domain.Calculator_{invest.index.ToString()}"
            );

            if (calcType == null)
            {
                throw new ExpectedException($"Tipo de calculo nao encontrado: Calculator_{invest.index}");
            }

            var calc = (ICalculator)Activator.CreateInstance(calcType, _context)!;

            invest.calculated = calc.Calculate(invest.ToRequest());
            invest.table_calculated = BuildTableCalculated(invest);
            if (invest.archived)
            {
                invest.table_value = invest.value;
                continue;
            }

            var redeemedTotal = invest.redemptions?.Sum(r => r.value) ?? 0m;
            var currentValueLiq = invest.calculated.FirstOrDefault()?.value_liq ?? 0m;
            var currentRatio = GetDisplayRedemptionRatio(redeemedTotal, currentValueLiq);
            invest.table_value = Math.Max(0m, invest.value - (invest.value * currentRatio));
        }


        return investments;
    }

    /// <summary>
    /// Retorna os rendimentos líquidos e os impostos gerados pela carteira em cada mês.
    /// Os 12 meses futuros são estimados com as taxas disponíveis atualmente.
    /// </summary>
    [HttpGet("Investments/monthly-tax-projection")]
    [ProducesResponseType(typeof(List<MonthlyTaxProjectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public List<MonthlyTaxProjectionResponse> GetMonthlyTaxProjection([FromQuery] Guid? wallet_id = null)
    {
        var wallet = WalletAccess.ResolveAccessibleWallet(_context, _user, wallet_id);
        var investments = _context.investments
            .AsNoTracking()
            .Include(investment => investment.bank)
            .Include(investment => investment.redemptions)
            .Where(investment =>
                investment.owner.id == _user.id &&
                (investment.wallet_id == wallet.id || investment.wallet_id == null))
            .ToList();

        var now = DateTime.UtcNow;
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var calculators = new Dictionary<IdexesType, ICalculator>();
        var result = new List<MonthlyTaxProjectionResponse>();

        // Do mês atual voltamos 11 meses; depois projetamos os 12 meses seguintes.
        for (var monthOffset = -11; monthOffset <= 12; monthOffset++)
        {
            var monthStart = currentMonth.AddMonths(monthOffset);
            var cutoff = monthOffset == 0 ? now : monthStart.AddMonths(1).AddTicks(-1);
            decimal liquidEarnings = 0m;
            decimal irValue = 0m;
            decimal iofValue = 0m;

            foreach (var investment in investments)
            {
                if (investment.date_buy > cutoff)
                    continue;

                var periodStart = investment.date_buy > monthStart
                    ? investment.date_buy
                    : monthStart;
                var periodEnd = investment.due_date.HasValue && investment.due_date.Value < cutoff
                    ? investment.due_date.Value
                    : cutoff;

                if (periodEnd <= periodStart)
                    continue;

                if (!calculators.TryGetValue(investment.index, out var calculator))
                {
                    var calculatorType = typeof(ICalculator).Assembly.GetType(
                        $"server.Domain.Calculator_{investment.index}");

                    if (calculatorType == null)
                        throw new ExpectedException($"Tipo de calculo nao encontrado: Calculator_{investment.index}");

                    calculator = (ICalculator)Activator.CreateInstance(calculatorType, _context)!;
                    calculators[investment.index] = calculator;
                }

                var monthlyValues = CalculateMonthlyEarnings(
                    investment,
                    periodStart,
                    periodEnd,
                    calculator);

                liquidEarnings += monthlyValues.LiquidEarnings;
                irValue += monthlyValues.IrValue;
                iofValue += monthlyValues.IofValue;
            }

            var roundedLiquidValue = Math.Round(liquidEarnings, 2);
            var roundedIrValue = Math.Round(irValue, 2);
            var roundedIofValue = Math.Round(iofValue, 2);

            result.Add(new MonthlyTaxProjectionResponse(
                monthStart.ToString("yyyy-MM"),
                roundedLiquidValue,
                roundedIrValue,
                roundedIofValue,
                roundedIrValue + roundedIofValue,
                monthOffset > 0));
        }

        return result;
    }

    private static MonthlyEarnings CalculateMonthlyEarnings(
        Investment investment,
        DateTime periodStart,
        DateTime periodEnd,
        ICalculator calculator)
    {
        var liquidEarnings = 0m;
        var irValue = 0m;
        var iofValue = 0m;
        var segmentStart = periodStart;
        var remainingShare = GetRemainingShareAtDate(investment, periodStart, calculator);

        foreach (var redemption in investment.redemptions
            .Where(redemption => redemption.value > 0m && redemption.date > periodStart && redemption.date <= periodEnd)
            .OrderBy(redemption => redemption.date))
        {
            AddEarningsSegment(
                investment,
                segmentStart,
                redemption.date,
                remainingShare,
                calculator,
                ref liquidEarnings,
                ref irValue,
                ref iofValue);

            remainingShare = GetRemainingShareAtDate(investment, redemption.date, calculator);
            segmentStart = redemption.date;
        }

        AddEarningsSegment(
            investment,
            segmentStart,
            periodEnd,
            remainingShare,
            calculator,
            ref liquidEarnings,
            ref irValue,
            ref iofValue);

        return new MonthlyEarnings(liquidEarnings, irValue, iofValue);
    }

    private static void AddEarningsSegment(
        Investment investment,
        DateTime segmentStart,
        DateTime segmentEnd,
        decimal remainingShare,
        ICalculator calculator,
        ref decimal liquidEarnings,
        ref decimal irValue,
        ref decimal iofValue)
    {
        if (segmentEnd <= segmentStart || remainingShare <= 0m)
            return;

        var request = investment.ToRequest();
        var startProfit = segmentStart <= investment.date_buy
            ? 0m
            : calculator.Generate(request, segmentStart).profit_brute;
        var endCalculated = calculator.Generate(request, segmentEnd);
        var grossEarnings = Math.Max(0m, endCalculated.profit_brute - startProfit) * remainingShare;

        if (grossEarnings <= 0m)
            return;

        var segmentIof = grossEarnings * Math.Clamp(endCalculated.IOF / 100m, 0m, 1m);
        var segmentIr = (grossEarnings - segmentIof) * Math.Clamp(endCalculated.IR / 100m, 0m, 1m);

        iofValue += segmentIof;
        irValue += segmentIr;
        liquidEarnings += grossEarnings - segmentIof - segmentIr;
    }

    private static decimal GetRemainingShareAtDate(
        Investment investment,
        DateTime cutoff,
        ICalculator calculator)
    {
        var remainingShare = 1m;
        var request = investment.ToRequest();

        foreach (var redemption in investment.redemptions
            .Where(redemption => redemption.value > 0m && redemption.date <= cutoff)
            .OrderBy(redemption => redemption.date))
        {
            if (redemption.date < investment.date_buy)
                continue;

            var redemptionDate = investment.due_date.HasValue && redemption.date > investment.due_date.Value
                ? investment.due_date.Value
                : redemption.date;
            var valueBeforeRedemption = Math.Max(0m, calculator.Generate(request, redemptionDate).value_liq)
                * remainingShare;

            if (valueBeforeRedemption <= 0m)
                continue;

            var redeemedShare = Math.Clamp(redemption.value / valueBeforeRedemption, 0m, 1m);
            remainingShare *= 1m - redeemedShare;
        }

        return remainingShare;
    }

    [HttpGet("Investments/limits")]
    [ProducesResponseType(typeof(InvestmentLimitOverviewResponse), StatusCodes.Status200OK)]
    public InvestmentLimitOverviewResponse GetInvestmentLimits() => BuildInvestmentLimitOverview();




    /// <summary>
    /// Lista todos os investimentos do usuário
    /// </summary>
    /// <returns>Lista todos os investimentos do usuário</returns>
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet("Investments/{id}")]
    public Investment Get(Guid id)
    {
        var investment = GetInvestmentByID(id);
        if (investment == null)
            throw new ExpectedException("Investimento não encontrado.");

        WalletAccess.ResolveAccessibleWallet(_context, _user, investment.wallet_id);
        return investment;
    }


    /// <summary>
	/// Adiciona um novo investimento
	/// </summary>
	/// <returns>Retono com o id do investimento</returns>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPost("Investments")]
    public Guid Insert([FromBody] InvestmentRequest request)
    {
        var now = DateTime.UtcNow;
        var investmentLimit = BuildInvestmentLimitOverview();
        var replacementSource = GetValidReplacementSource(request.replacement_source_investment_id);

        if (!investmentLimit.can_create && replacementSource is null)
            throw new ExpectedException(
                investmentLimit.restriction_message ?? "Seu plano atual atingiu o limite de investimentos ativos. Faça upgrade para adicionar novos investimentos.",
                HttpStatusCode.Forbidden);

        if (request.ai_extracted)
        {
            var plan = SubscriptionFeatureAccess.GetEffectivePlan(_context, _user.id);
            var aiUsageCount = SubscriptionFeatureAccess.GetAiUsageCountInMonth(
                _context,
                _user.id,
                SubscriptionFeatureAccess.InvestmentDocumentExtractionFeature,
                now);

            if (aiUsageCount >= plan.ai_monthly_limit)
                throw new ExpectedException(
                    $"Seu plano {plan.name} permite {plan.ai_monthly_limit} leituras de comprovantes por mês. Faça upgrade para usar este recurso.",
                    System.Net.HttpStatusCode.Forbidden);
        }

        var bank = _context.banks.FirstOrDefault(b => b.Code == (ushort)request.bank_code)
            ?? throw new ExpectedException($"Banco com código {request.bank_code} não encontrado.");
        var wallet = WalletAccess.ResolveAccessibleWallet(_context, _user, request.wallet_id);
        var moneyBox = ResolveMoneyBox(request.money_box_id);
        EnsureMoneyBoxSelectionAllowed(request.money_box_id);

        Investment investment = new Investment(request, _user, bank);
        _context.Entry(investment.owner).State = EntityState.Unchanged;
        _context.Entry(investment.bank).State = EntityState.Unchanged;
        investment.wallet = wallet;
        investment.wallet_id = wallet.id;
        investment.money_box = moneyBox;
        _context.investments.Add(investment);

        if (replacementSource is not null)
        {
            replacementSource.archived = true;
            _context.investments.Update(replacementSource);
        }

        if (request.ai_extracted)
        {
            var provider = (Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "openai").Trim().ToLowerInvariant();
            _context.ai_usages.Add(new AiUsage
            {
                user_id = _user.id,
                feature = SubscriptionFeatureAccess.InvestmentDocumentExtractionFeature,
                provider = provider,
                created_at = now
            });
        }

        _context.SaveChanges();

        return investment.id;
    }

    /// <summary>
    /// Extrai campos de investimento a partir de um arquivo enviado para a IA.
    /// </summary>
    [ProducesResponseType(typeof(InvestmentDocumentExtractionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPost("Investments/extract")]
    public async Task<InvestmentDocumentExtractionResult> ExtractInvestmentFromFile(
        [FromForm] InvestmentDocumentUploadRequest request,
        [FromServices] IInvestmentDocumentExtractor extractor,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var plan = SubscriptionFeatureAccess.GetEffectivePlan(_context, _user.id);
        var aiUsageCount = SubscriptionFeatureAccess.GetAiUsageCountInMonth(
            _context,
            _user.id,
            SubscriptionFeatureAccess.InvestmentDocumentExtractionFeature,
            now);

        if (aiUsageCount >= plan.ai_monthly_limit)
            throw new ExpectedException(
                $"Seu plano {plan.name} permite {plan.ai_monthly_limit} leituras de comprovantes por mês. Faça upgrade para usar este recurso.",
                System.Net.HttpStatusCode.Forbidden);

        var banks = _context.banks
            .AsNoTracking()
            .Where(bank => bank.Active)
            .ToList();

        return await extractor.ExtractAsync(request.file, banks, cancellationToken);
    }



    /// <summary>
    /// Edita um investimento já adicionado anteiormente
    /// </summary>
    /// <returns>Retono vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPatch("Investments/{id}")]
    public IActionResult Update(Guid id, [FromBody] InvestmentRequest request)
    {
        try
        {
            var bank = _context.banks.FirstOrDefault(b => b.Code == (ushort)request.bank_code)
                ?? throw new ExpectedException($"Banco com código {request.bank_code} não encontrado.");

            Investment investment = GetInvestmentByID(id);
            if (investment == null)
                throw new ExpectedException("Investimento não encontrado.");

            var wallet = WalletAccess.ResolveAccessibleWallet(_context, _user, request.wallet_id ?? investment.wallet_id);
            var moneyBox = ResolveMoneyBox(request.money_box_id);
            EnsureMoneyBoxSelectionAllowed(request.money_box_id, investment.money_box_id);
            investment.Update(request, bank);
            investment.wallet = wallet;
            investment.wallet_id = wallet.id;
            investment.money_box = moneyBox;
            _context.investments.Update(investment);
            _context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            throw new ExpectedException(e.Message);
        }
    }

    /// <summary>
    /// Arquiva ou desarquiva um investimento.
    /// </summary>
    /// <returns>Retorno vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPatch("Investments/{id}/archive")]
    public IActionResult Archive(Guid id, [FromBody] ArchiveInvestmentRequest request)
    {
        var investment = GetInvestmentByID(id);

        if (investment == null)
            throw new ExpectedException("Investimento não encontrado.");

        WalletAccess.ResolveAccessibleWallet(_context, _user, investment.wallet_id);

        if (request.archived && !CanArchiveOrReinvestBeforeDueDate(investment.due_date))
            throw new ExpectedException("Somente investimentos vencidos ou a até 5 dias corridos do vencimento podem ser arquivados.");

        investment.archived = request.archived;
        _context.investments.Update(investment);
        _context.SaveChanges();

        return NoContent();
    }


    /// <summary>
    /// Remove um investimento
    /// </summary>
    /// <returns>Retono vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpDelete("Investments/{id}")]
    public void Delete(Guid id)
    {
        var investment = GetInvestmentByID(id);

        if (investment == null)
            throw new ExpectedException("Investimento não encontrado ou já removido");

        WalletAccess.ResolveAccessibleWallet(_context, _user, investment.wallet_id);

        _context.investments.Remove(investment);
        _context.SaveChanges();

    }


    /// <summary>
    /// Resgata um investimento parcial ou totalmente
    /// </summary>
    /// <returns>Retono vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPut("Investments/{id}")]
    public Investment Redeem(Guid id, [FromBody] RedemptionRequest request)
    {
        var invest = _context.investments.Where(x => x.id == id && x.owner.id == _user.id && x.id == id).FirstOrDefault();
        if (invest == null)
            throw new ExpectedException("Investimento não encontrado.");

        WalletAccess.ResolveAccessibleWallet(_context, _user, invest.wallet_id);

        Redemption redemption = new Redemption(invest, request);

        _context.redemptions.Add(redemption);
        _context.SaveChanges();
        ArchiveIfFullyRedeemed(invest);
        _context.SaveChanges();

        return invest;
    }

    /// <summary>
    /// Edita um resgate já registrado
    /// </summary>
    /// <returns>Retorno vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpPatch("Redemptions/{id}")]
    public IActionResult UpdateRedemption(Guid id, [FromBody] RedemptionRequest request)
    {
        var redemption = _context.redemptions
            .Include(r => r.investment)
            .ThenInclude(i => i.owner)
            .FirstOrDefault(r => r.id == id && r.investment.owner.id == _user.id);

        if (redemption == null)
            throw new ExpectedException("Resgate não encontrado.");

        WalletAccess.ResolveAccessibleWallet(_context, _user, redemption.investment.wallet_id);

        redemption.title = request.title;
        redemption.value = request.value;
        redemption.date = DateTime.SpecifyKind(request.date, DateTimeKind.Utc);

        _context.redemptions.Update(redemption);
        _context.SaveChanges();
        ArchiveIfFullyRedeemed(redemption.investment);
        _context.SaveChanges();

        return NoContent();
    }

    /// <summary>
    /// Remove um resgate já registrado
    /// </summary>
    /// <returns>Retorno vazio</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpDelete("Redemptions/{id}")]
    public IActionResult DeleteRedemption(Guid id)
    {
        var redemption = _context.redemptions
            .Include(r => r.investment)
            .ThenInclude(i => i.owner)
            .FirstOrDefault(r => r.id == id && r.investment.owner.id == _user.id);

        if (redemption == null)
            throw new ExpectedException("Resgate não encontrado.");

        WalletAccess.ResolveAccessibleWallet(_context, _user, redemption.investment.wallet_id);

        _context.redemptions.Remove(redemption);
        _context.SaveChanges();

        return NoContent();
    }

}

public record InvestmentLimitOverviewResponse(
    int active_investments_count,
    int? investments_limit,
    bool can_create,
    bool is_over_limit,
    string active_plan_id,
    string active_plan_name,
    string? restriction_message
);

public record MonthlyTaxProjectionResponse(
    string month,
    decimal liquid_value,
    decimal ir_value,
    decimal iof_value,
    decimal taxes_value,
    bool estimated
);

internal record MonthlyEarnings(
    decimal LiquidEarnings,
    decimal IrValue,
    decimal IofValue
);
