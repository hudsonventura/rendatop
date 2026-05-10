using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class DashboardPlaceholderPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly SessionService _session;

    public DashboardPlaceholderPage(AuthService auth, SessionService session)
    {
        _auth = auth;
        _session = session;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var name = string.IsNullOrWhiteSpace(_session.Name) ? "Usuario" : _session.Name;
        WelcomeLabel.Text = $"Ola, {name}";
        EmailLabel.Text = _session.Email;
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        LogoutButton.IsEnabled = false;
        LogoutButton.Text = "Saindo...";

        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//login");

        LogoutButton.Text = "Sair";
        LogoutButton.IsEnabled = true;
    }
}
