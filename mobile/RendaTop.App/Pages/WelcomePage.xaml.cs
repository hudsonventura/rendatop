using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class WelcomePage : ContentPage
{
    private readonly AuthService _auth;

    public WelcomePage(AuthService auth)
    {
        _auth = auth;
        InitializeComponent();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(LoginPage));

    private async void OnSignupClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SignupPage));

    private async void OnGoogleClicked(object? sender, EventArgs e)
    {
        HideError();
        SetExternalAuthBusy(true, "Conectando ao Google...", "google");

        try
        {
            await _auth.LoginWithGoogleAsync();
            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel concluir o login com Google. Verifique sua conexao e tente novamente.");
        }
        finally
        {
            SetExternalAuthBusy(false);
        }
    }

    private async void OnMicrosoftClicked(object? sender, EventArgs e)
    {
        HideError();
        SetExternalAuthBusy(true, "Conectando a Microsoft...", "microsoft");

        try
        {
            await _auth.LoginWithMicrosoftAsync();
            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel concluir o login com Microsoft. Verifique sua conexao e tente novamente.");
        }
        finally
        {
            SetExternalAuthBusy(false);
        }
    }

    private void SetExternalAuthBusy(bool busy, string? text = null, string provider = "google")
    {
        LoginButton.IsEnabled = !busy;
        SignupButton.IsEnabled = !busy;
        GoogleLoginButton.IsEnabled = !busy;
        MicrosoftLoginButton.IsEnabled = !busy;

        GoogleLoginButton.Text = busy && string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase)
            ? text ?? "Conectando..."
            : "Login com Google / GMail";

        MicrosoftLoginButton.Text = busy && string.Equals(provider, "microsoft", StringComparison.OrdinalIgnoreCase)
            ? text ?? "Conectando..."
            : "Entrar com a Microsoft / Outlook";
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
    }

    private void HideError()
    {
        ErrorLabel.Text = string.Empty;
        ErrorBorder.IsVisible = false;
    }
}
