using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;
using System.Net;

namespace server.Controllers;

[ApiController]
public class WalletsController : AuthenticatedController
{
    private readonly Context _context;

    public WalletsController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("Wallets")]
    [ProducesResponseType(typeof(WalletsOverviewResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var user = _context.users.FirstOrDefault(item => item.id == _user.id)
            ?? throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        WalletAccess.EnsureDefaultWallet(_context, user);

        var wallets = _context.wallets
            .AsNoTracking()
            .Where(item => item.owner_id == _user.id)
            .OrderBy(item => item.created_at)
            .ThenBy(item => item.id)
            .ToList();

        var enabledWalletIds = SubscriptionFeatureAccess.GetEnabledWalletIds(_context, _user.id);
        var limit = SubscriptionFeatureAccess.GetWalletsLimit(_context, _user.id);
        var plan = SubscriptionFeatureAccess.GetEffectivePlan(_context, _user.id);
        var canCreate = SubscriptionFeatureAccess.CanCreateWallets(_context, _user.id, wallets.Count);
        var activeWallet = wallets.FirstOrDefault(item => enabledWalletIds.Contains(item.id)) ?? wallets.First();

        return Ok(new WalletsOverviewResponse(
            wallets
                .Select(item => new WalletResponse(
                    item.id,
                    item.name,
                    enabledWalletIds.Contains(item.id),
                    item.created_at,
                    item.updated_at))
                .ToList(),
            activeWallet.id,
            wallets.Count,
            limit == int.MaxValue ? null : limit,
            canCreate,
            plan.id,
            BuildRestrictionMessage(plan, wallets.Count, limit, canCreate)));
    }

    [HttpPost("Wallets")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] WalletRequest request)
    {
        var user = _context.users.FirstOrDefault(item => item.id == _user.id)
            ?? throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        WalletAccess.EnsureDefaultWallet(_context, user);

        var existingCount = _context.wallets.Count(item => item.owner_id == _user.id);
        var plan = SubscriptionFeatureAccess.GetEffectivePlan(_context, _user.id);
        if (!SubscriptionFeatureAccess.CanCreateWallets(_context, _user.id, existingCount))
            throw new ExpectedException($"Seu plano {plan.name} permite {DescribeWalletLimit(plan.wallets_limit)}. Faça upgrade para criar novas carteiras.", HttpStatusCode.Forbidden);

        var normalizedName = NormalizeName(request.name);
        EnsureUniqueName(normalizedName, null);

        var wallet = new Wallet
        {
            owner = user,
            owner_id = user.id,
            name = normalizedName,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        _context.wallets.Add(wallet);
        _context.SaveChanges();

        return Ok(new WalletResponse(wallet.id, wallet.name, true, wallet.created_at, wallet.updated_at));
    }

    [HttpPatch("Wallets/{id}")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    public IActionResult Update(Guid id, [FromBody] WalletRequest request)
    {
        var wallet = _context.wallets.FirstOrDefault(item => item.id == id && item.owner_id == _user.id)
            ?? throw new ExpectedException("Carteira não encontrada.", HttpStatusCode.NotFound);

        var normalizedName = NormalizeName(request.name);
        EnsureUniqueName(normalizedName, id);

        wallet.name = normalizedName;
        wallet.updated_at = DateTime.UtcNow;
        _context.SaveChanges();

        var enabled = SubscriptionFeatureAccess.CanAccessWallet(_context, _user.id, wallet.id);
        return Ok(new WalletResponse(wallet.id, wallet.name, enabled, wallet.created_at, wallet.updated_at));
    }

    private void EnsureUniqueName(string normalizedName, Guid? currentId)
    {
        var exists = _context.wallets
            .AsNoTracking()
            .Any(item =>
                item.owner_id == _user.id &&
                item.id != currentId &&
                item.name.ToLower() == normalizedName.ToLower());

        if (exists)
            throw new ExpectedException("Você já possui uma carteira com esse nome.");
    }

    private static string NormalizeName(string? name)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ExpectedException("Nome da carteira é obrigatório.");

        return normalizedName;
    }

    private static string? BuildRestrictionMessage(Plan plan, int count, int limit, bool canCreate)
    {
        if (plan.id == "pro")
            return null;

        if (!canCreate)
            return $"Seu plano atual permite {DescribeWalletLimit(limit)}. Carteiras excedentes ficam indisponíveis até o upgrade.";

        return $"Seu plano atual permite {DescribeWalletLimit(limit)}. Você está usando {count} de {limit}.";
    }

    private static string DescribeWalletLimit(int limit) =>
        limit == int.MaxValue ? "carteiras ilimitadas" : limit == 1 ? "1 carteira" : $"até {limit} carteiras";
}

public record WalletRequest(string name);

public record WalletResponse(
    Guid id,
    string name,
    bool enabled,
    DateTime created_at,
    DateTime updated_at
);

public record WalletsOverviewResponse(
    List<WalletResponse> items,
    Guid active_wallet_id,
    int count,
    int? limit,
    bool can_create,
    string active_plan_id,
    string? restriction_message
);
