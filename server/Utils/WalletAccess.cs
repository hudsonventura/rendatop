using System.Net;
using Microsoft.EntityFrameworkCore;
using server.Domain;

namespace server.Utils;

public static class WalletAccess
{
    public static Wallet EnsureDefaultWallet(Context context, User user)
    {
        var existing = context.wallets
            .OrderBy(wallet => wallet.created_at)
            .ThenBy(wallet => wallet.id)
            .FirstOrDefault(wallet => wallet.owner_id == user.id);

        if (existing is not null)
            return existing;

        var wallet = new Wallet
        {
            owner_id = user.id,
            name = Wallet.DefaultName,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        context.wallets.Add(wallet);
        context.SaveChanges();
        return wallet;
    }

    public static Wallet ResolveAccessibleWallet(Context context, User user, Guid? walletId)
    {
        var defaultWallet = EnsureDefaultWallet(context, user);

        var requestedId = walletId ?? context.wallets
            .AsNoTracking()
            .Where(wallet => wallet.owner_id == user.id)
            .OrderBy(wallet => wallet.created_at)
            .ThenBy(wallet => wallet.id)
            .Select(wallet => wallet.id)
            .First();

        var wallet = context.wallets
            .FirstOrDefault(item => item.id == requestedId && item.owner_id == user.id)
            ?? defaultWallet;

        if (!SubscriptionFeatureAccess.CanAccessWallet(context, user.id, wallet.id))
            throw new ExpectedException("Esta carteira está indisponível no seu plano atual. Faça upgrade para acessá-la.", HttpStatusCode.Forbidden);

        return wallet;
    }
}
