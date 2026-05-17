using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SplashPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ConnectivityService _connectivity;
    private bool _started;

    public SplashPage(SessionService session, ConnectivityService connectivity)
    {
        _session = session;
        _connectivity = connectivity;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_started)
            return;

        _started = true;
        await Task.Delay(TimeSpan.FromSeconds(1));
        await _session.InitializeAsync();

        var isAuthenticated = _connectivity.IsOffline
            ? await _session.HasOfflineSessionAsync()
            : await _session.IsAuthenticatedAsync();
        await Shell.Current.GoToAsync(isAuthenticated ? "//dashboard" : "//welcome");
    }
}
