using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SplashPage : ContentPage
{
    private readonly SessionService _session;
    private bool _started;

    public SplashPage(SessionService session)
    {
        _session = session;
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

        var isAuthenticated = await _session.IsAuthenticatedAsync();
        await Shell.Current.GoToAsync(isAuthenticated ? "//dashboard" : "//login");
    }
}
