namespace RendaTop.App.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(LoginPage));

    private async void OnSignupClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SignupPage));
}
