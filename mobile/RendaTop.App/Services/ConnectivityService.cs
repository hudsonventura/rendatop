namespace RendaTop.App.Services;

public sealed class ConnectivityService
{
    public bool HasInternetAccess => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public bool IsOffline => !HasInternetAccess;
}
