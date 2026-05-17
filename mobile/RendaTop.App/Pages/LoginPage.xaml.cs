using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private string _totpChallengeId = string.Empty;
    private bool _totpRequired;

    public LoginPage(AuthService auth)
    {
        _auth = auth;
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        HideError();
        SetBusy(true);

        try
        {
            if (_totpRequired)
            {
                await _auth.LoginTotpAsync(_totpChallengeId, TotpEntry.Text ?? string.Empty);
                await Shell.Current.GoToAsync("//dashboard");
                return;
            }

            var response = await _auth.LoginAsync(EmailEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);
            if (response.RequiresTotp)
            {
                _totpChallengeId = response.ChallengeId ?? string.Empty;
                if (string.IsNullOrWhiteSpace(_totpChallengeId))
                    throw new ApiException("Desafio TOTP ausente. Tente entrar novamente.", 400);

                ShowTotpStep();
                return;
            }

            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (ApiException ex)
        {
            if (_totpRequired && ex.Message.Contains("expirado", StringComparison.OrdinalIgnoreCase))
                ResetTotpStep();

            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel entrar. Verifique sua conexao e tente novamente.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordVisibilityButton.Text = PasswordEntry.IsPassword ? "Mostrar" : "Ocultar";
    }

    private async void OnGoogleClicked(object? sender, EventArgs e)
        => await Launcher.Default.OpenAsync(_auth.GoogleLoginUri);

    private async void OnMicrosoftClicked(object? sender, EventArgs e)
        => await Launcher.Default.OpenAsync(_auth.MicrosoftLoginUri);

    private async void OnSignupClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SignupPage));

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private void ShowTotpStep()
    {
        _totpRequired = true;
        TotpSection.IsVisible = true;
        EmailEntry.IsEnabled = false;
        PasswordEntry.IsEnabled = false;
        PasswordVisibilityButton.IsEnabled = false;
        TitleLabel.Text = "Codigo de acesso";
        DescriptionLabel.Text = "Email e senha validados. Informe o codigo do autenticador.";
        LoginButton.Text = "Validar codigo";
    }

    private void ResetTotpStep()
    {
        _totpRequired = false;
        _totpChallengeId = string.Empty;
        TotpEntry.Text = string.Empty;
        TotpSection.IsVisible = false;
        EmailEntry.IsEnabled = true;
        PasswordEntry.IsEnabled = true;
        PasswordVisibilityButton.IsEnabled = true;
        TitleLabel.Text = "Bem-vindo";
        DescriptionLabel.Text = "Informe seus dados para acessar sua conta";
        LoginButton.Text = "Entrar";
    }

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        LoginButton.Text = busy ? "Entrando..." : _totpRequired ? "Validar codigo" : "Entrar";
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

    private static async Task NavigateBackAsync()
    {
        var shell = Shell.Current;
        if (shell is null)
            return;

        if (shell.Navigation.NavigationStack.Count > 1)
        {
            await shell.GoToAsync("..");
            return;
        }

        await shell.GoToAsync("//welcome");
    }
}
