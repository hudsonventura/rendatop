using RendaTop.App.Services;

namespace RendaTop.App.Pages;

[QueryProperty(nameof(Email), "email")]
[QueryProperty(nameof(Message), "message")]
public partial class SignupVerificationPage : ContentPage
{
    private readonly AuthService _auth;
    private string _email = string.Empty;
    private string _message = "Informe o codigo enviado por email para ativar sua conta.";

    public SignupVerificationPage(AuthService auth)
    {
        _auth = auth;
        InitializeComponent();
        ApplyState();
    }

    public string Email
    {
        get => _email;
        set
        {
            _email = Uri.UnescapeDataString(value ?? string.Empty);
            ApplyState();
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            _message = Uri.UnescapeDataString(value ?? string.Empty);
            ApplyState();
        }
    }

    private async void OnVerifyClicked(object? sender, EventArgs e)
    {
        HideError();

        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            ShowError("Email pendente nao informado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CodeEntry.Text))
        {
            ShowError("Codigo de verificacao e obrigatorio.");
            return;
        }

        SetVerifyBusy(true);
        try
        {
            await _auth.VerifySignupAsync(EmailEntry.Text, CodeEntry.Text);
            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel ativar a conta. Verifique sua conexao e tente novamente.");
        }
        finally
        {
            SetVerifyBusy(false);
        }
    }

    private async void OnResendClicked(object? sender, EventArgs e)
    {
        HideError();
        SetResendBusy(true);

        try
        {
            MessageLabel.Text = await _auth.ResendSignupVerificationAsync(EmailEntry.Text);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel reenviar o codigo. Verifique sua conexao e tente novamente.");
        }
        finally
        {
            SetResendBusy(false);
        }
    }

    private void ApplyState()
    {
        if (EmailEntry is null || MessageLabel is null)
            return;

        EmailEntry.Text = _email;
        MessageLabel.Text = string.IsNullOrWhiteSpace(_message)
            ? "Informe o codigo enviado por email para ativar sua conta."
            : _message;
    }

    private void SetVerifyBusy(bool busy)
    {
        VerifyButton.IsEnabled = !busy;
        VerifyButton.Text = busy ? "Ativando conta..." : "Ativar conta";
    }

    private void SetResendBusy(bool busy)
    {
        ResendButton.IsEnabled = !busy;
        ResendButton.Text = busy ? "Reenviando codigo..." : "Reenviar codigo";
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
