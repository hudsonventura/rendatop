using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SignupPage : ContentPage
{
    private readonly AuthService _auth;

    public SignupPage(AuthService auth)
    {
        _auth = auth;
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
    }

    private async void OnSignupClicked(object? sender, EventArgs e)
    {
        HideError();

        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmation = ConfirmPasswordEntry.Text ?? string.Empty;
        var passwordError = PasswordPolicy.Validate(password, confirmation);

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Nome e obrigatorio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Email e obrigatorio.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(passwordError))
        {
            ShowError(passwordError);
            return;
        }

        SetBusy(true);
        try
        {
            var response = await _auth.SignupAsync(name, email, password);
            var route = $"{nameof(SignupVerificationPage)}?email={Uri.EscapeDataString(response.Email)}&message={Uri.EscapeDataString(response.Message)}";
            await Shell.Current.GoToAsync(route);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel criar a conta. Verifique sua conexao e tente novamente.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private void SetBusy(bool busy)
    {
        SignupButton.IsEnabled = !busy;
        SignupButton.Text = busy ? "Criando conta..." : "Criar conta";
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
