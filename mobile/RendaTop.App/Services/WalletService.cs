using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class WalletService
{
    private const string ActiveWalletKey = "rendatop.active_wallet_id";

    private readonly ApiClient _apiClient;

    public WalletService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public event EventHandler<Guid>? ActiveWalletChanged;

    public Guid? ActiveWalletId
    {
        get
        {
            var raw = Preferences.Default.Get(ActiveWalletKey, string.Empty);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public async Task<WalletsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var overview = await _apiClient.GetAsync<WalletsOverviewDto>("/Wallets", cancellationToken)
            ?? new WalletsOverviewDto { Items = [] };

        var enabled = overview.Items?.Where(item => item.Enabled).ToList() ?? [];
        var current = ActiveWalletId;
        var active = enabled.Any(item => item.Id == current)
            ? current!.Value
            : overview.ActiveWalletId != Guid.Empty
                ? overview.ActiveWalletId
                : enabled.FirstOrDefault()?.Id ?? Guid.Empty;

        if (active != Guid.Empty && active != current)
            SetActiveWallet(active);

        return overview;
    }

    public async Task<WalletDto> CreateAsync(string name, CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<WalletRequestDto, WalletDto>(
                "/Wallets",
                new WalletRequestDto(name.Trim()),
                cancellationToken)
           ?? throw new ApiException("Resposta de carteira invalida.", 500);

    public void SetActiveWallet(Guid walletId)
    {
        if (walletId == Guid.Empty || walletId == ActiveWalletId)
            return;

        Preferences.Default.Set(ActiveWalletKey, walletId.ToString());
        ActiveWalletChanged?.Invoke(this, walletId);
    }

    public string WithActiveWallet(string path)
    {
        var walletId = ActiveWalletId;
        if (!walletId.HasValue)
            return path;

        var separator = path.Contains('?') ? "&" : "?";
        return $"{path}{separator}wallet_id={walletId.Value}";
    }

    public WalletRequestDto AttachToRequest(WalletRequestDto request) => request;
}
