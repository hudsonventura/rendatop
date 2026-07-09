using System.Text.RegularExpressions;
using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SettingsPage : ContentPage
{
    private const string PasswordPolicyMessage = "A senha deve ter no minimo 9 caracteres, incluindo pelo menos 1 letra, 1 numero e 1 caractere especial.";

    private readonly UserSettingsService _service;
    private readonly SessionService _session;
    private readonly ConnectivityService _connectivity;
    private readonly NotificationTitleView _titleView;
    private UserSettingsDto? _settings;
    private bool _isAdminUser;

    public SettingsPage(UserSettingsService service, SessionService session, ConnectivityService connectivity, NotificationService notifications)
    {
        _service = service;
        _session = session;
        _connectivity = connectivity;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Configuracoes", notifications);
        UpdateAdminOnlyTestButtons();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        SetBusy(true);
        ClearMessages();

        try
        {
            _settings = _connectivity.IsOffline
                ? await _service.GetCachedAsync()
                : await _service.GetAsync();

            if (_settings is null)
                throw new ApiException("Nao ha configuracoes disponiveis offline ainda.", 404);

            BindSettings(_settings);
            ApplyOfflineMode(_connectivity.IsOffline);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar suas configuracoes.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BindSettings(UserSettingsDto data)
    {
        NameEntry.Text = data.Name;
        EmailEntry.Text = data.PendingEmail ?? data.Email;
        EmailHintLabel.Text = string.IsNullOrWhiteSpace(data.PendingEmail)
            ? string.Empty
            : $"Seu email atual continua sendo {data.Email} ate voce confirmar o codigo enviado para {data.PendingEmail}.";
        EmailHintLabel.IsVisible = !string.IsNullOrWhiteSpace(EmailHintLabel.Text);

        PendingEmailBorder.IsVisible = !string.IsNullOrWhiteSpace(data.PendingEmail);
        PendingEmailInfoLabel.Text = !string.IsNullOrWhiteSpace(data.PendingEmail)
            ? $"Confirme o codigo enviado para {data.PendingEmail}."
            : string.Empty;

        PhoneEntry.Text = data.Phone;
        TelegramChatIdEntry.Text = data.TelegramChatId ?? string.Empty;
        CpfEntry.Text = FormatCpf(data.Cpf);
        NotifyWhatsAppSwitch.IsToggled = data.WhatsappNotificationsEnabled && data.NotifyWhatsapp;
        NotifyWhatsAppSwitch.IsEnabled = data.WhatsappNotificationsEnabled;
        NotifyTelegramSwitch.IsToggled = data.NotifyTelegram;
        NotifyEmailSwitch.IsToggled = data.NotifyEmail;
        WhatsAppPremiumLabel.IsVisible = !data.WhatsappNotificationsEnabled;
        WhatsAppDescriptionLabel.Text = data.WhatsappNotificationsEnabled
            ? "Receber notificacoes por WhatsApp"
            : "Assine um plano para ativar este recurso.";

        CalendarPremiumLabel.IsVisible = !data.CalendarIcsEnabled;
        CalendarDescriptionLabel.Text = data.CalendarIcsEnabled
            ? "Gera um link publico para assinar no Outlook ou outro app de calendario."
            : "Assine um plano para ativar este recurso.";
        CalendarPublicSwitch.IsEnabled = data.CalendarIcsEnabled;
        CalendarPublicSwitch.IsToggled = data.CalendarIcsEnabled && data.CalendarPublicEnabled;
        CalendarUrlEntry.Text = data.CalendarPublicUrl ?? string.Empty;
        CalendarLinkSection.IsVisible = data.CalendarIcsEnabled && data.CalendarPublicEnabled;

        TotpDisabledSection.IsVisible = !data.TotpEnabled;
        TotpEnabledSection.IsVisible = data.TotpEnabled;
        TotpSetupSection.IsVisible = false;
        TotpSecretEntry.Text = string.Empty;
        TotpCodeEntry.Text = string.Empty;
        DisableTotpCodeEntry.Text = string.Empty;
        _isAdminUser = IsAdminUserType(data.UserType);
        UpdateAdminOnlyTestButtons();

        UpdatePasswordRequirements();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowError("Sem conexao. As configuracoes ficam disponiveis apenas para leitura no modo offline.");
            return;
        }

        ClearMessages();
        ClearChannelErrors();

        if (!TryBuildSaveRequest(out var request, out var error))
        {
            ShowError(error);
            return;
        }

        SetBusy(true);

        try
        {
            var response = await _service.UpdateAsync(request!);
            _settings = response;
            BindSettings(response);
            await _session.UpdateProfileAsync(response.Name, response.Email, response.UserType);

            if (!string.IsNullOrWhiteSpace(response.PendingEmail))
            {
                if (response.PendingEmailVerificationSent == false)
                    ShowSuccess("Seus dados foram salvos, mas nao conseguimos enviar o codigo agora. Tente reenviar abaixo.");
                else if (response.PendingEmailVerificationSent == true)
                    ShowSuccess("Enviamos um codigo para o novo email. Confirme-o abaixo para concluir a alteracao.");
                else
                    ShowSuccess("Suas configuracoes foram salvas. O novo email ainda precisa ser confirmado.");
            }
            else
            {
                ShowSuccess("Configuracoes salvas com sucesso.");
            }

            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
        }
        catch (ApiException ex)
        {
            SetChannelAwareError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel salvar suas configuracoes.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnTestWhatsAppClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowWhatsAppError("Sem conexao. Este teste so pode ser enviado online.");
            return;
        }

        await RunChannelActionAsync(
            TestWhatsAppButton,
            "Enviando...",
            () => _service.TestWhatsAppAsync(PhoneEntry.Text),
            ShowWhatsAppError);
    }

    private async void OnTestTelegramClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowTelegramError("Sem conexao. Este teste so pode ser enviado online.");
            return;
        }

        await RunChannelActionAsync(
            TestTelegramButton,
            "Enviando...",
            () => _service.TestTelegramAsync(TelegramChatIdEntry.Text),
            ShowTelegramError);
    }

    private async void OnTestEmailClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowEmailError("Sem conexao. Este teste so pode ser enviado online.");
            return;
        }

        await RunChannelActionAsync(
            TestEmailButton,
            "Enviando...",
            () => _service.TestEmailAsync(),
            ShowEmailError);
    }

    private async void OnShowTelegramGuideClicked(object? sender, EventArgs e)
        => await Navigation.PushModalAsync(new TelegramChatIdGuidePage());

    private async void OnVerifyPendingEmailClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowPendingEmailError("Sem conexao. A verificacao de email exige internet.");
            return;
        }

        PendingEmailErrorBorder.IsVisible = false;
        SetBusy(true);
        try
        {
            var response = await _service.VerifyPendingEmailAsync(EmailCodeEntry.Text?.Trim() ?? string.Empty);
            _settings = response;
            BindSettings(response);
            await _session.UpdateProfileAsync(response.Name, response.Email, response.UserType);
            ShowSuccess("Email atualizado e verificado com sucesso.");
        }
        catch (ApiException ex)
        {
            ShowPendingEmailError(ex.Message);
        }
        catch
        {
            ShowPendingEmailError("Nao foi possivel verificar o novo email.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnResendPendingEmailClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowPendingEmailError("Sem conexao. O reenvio do codigo exige internet.");
            return;
        }

        PendingEmailErrorBorder.IsVisible = false;
        SetBusy(true);
        try
        {
            ShowSuccess(await _service.ResendPendingEmailAsync());
        }
        catch (ApiException ex)
        {
            ShowPendingEmailError(ex.Message);
        }
        catch
        {
            ShowPendingEmailError("Nao foi possivel reenviar o codigo.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCancelPendingEmailClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowPendingEmailError("Sem conexao. O cancelamento exige internet.");
            return;
        }

        PendingEmailErrorBorder.IsVisible = false;
        SetBusy(true);
        try
        {
            var response = await _service.CancelPendingEmailAsync();
            _settings = response;
            BindSettings(response);
            await _session.UpdateProfileAsync(response.Name, response.Email, response.UserType);
            ShowSuccess("Alteracao de email cancelada.");
        }
        catch (ApiException ex)
        {
            ShowPendingEmailError(ex.Message);
        }
        catch
        {
            ShowPendingEmailError("Nao foi possivel cancelar a alteracao de email.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnGenerateTotpClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowError("Sem conexao. A configuracao de TOTP exige internet.");
            return;
        }

        ClearMessages();
        SetBusy(true);
        try
        {
            var setup = await _service.GenerateTotpAsync();
            TotpSecretEntry.Text = setup.Secret;
            TotpSetupSection.IsVisible = true;
            ShowSuccess("QR Code TOTP gerado. Cadastre no app autenticador e confirme o codigo.");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel gerar o QR Code TOTP.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnEnableTotpClicked(object? sender, EventArgs e)
    {
        ClearMessages();
        SetBusy(true);
        try
        {
            var response = await _service.EnableTotpAsync(TotpSecretEntry.Text ?? string.Empty, TotpCodeEntry.Text ?? string.Empty);
            _settings = response;
            BindSettings(response);
            await _session.UpdateProfileAsync(response.Name, response.Email, response.UserType);
            ShowSuccess("TOTP habilitado com sucesso.");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel habilitar TOTP.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnDisableTotpClicked(object? sender, EventArgs e)
    {
        ClearMessages();
        SetBusy(true);
        try
        {
            var response = await _service.DisableTotpAsync(DisableTotpCodeEntry.Text ?? string.Empty);
            _settings = response;
            BindSettings(response);
            await _session.UpdateProfileAsync(response.Name, response.Email, response.UserType);
            ShowSuccess("TOTP desabilitado.");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel desabilitar TOTP.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCopyCalendarLinkClicked(object? sender, EventArgs e)
    {
        var value = CalendarUrlEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return;

        await Clipboard.Default.SetTextAsync(value);
        ShowSuccess("Link do calendario copiado.");
    }

    private void OnNotifyWhatsAppToggled(object? sender, ToggledEventArgs e)
    {
        if (_settings is { WhatsappNotificationsEnabled: false })
        {
            NotifyWhatsAppSwitch.IsToggled = false;
            return;
        }

        if (e.Value && string.IsNullOrWhiteSpace(PhoneEntry.Text))
        {
            NotifyWhatsAppSwitch.IsToggled = false;
            SetChannelAwareError("Informe o telefone antes de habilitar o WhatsApp.");
        }
    }

    private void OnNotifyTelegramToggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value && string.IsNullOrWhiteSpace(TelegramChatIdEntry.Text))
        {
            NotifyTelegramSwitch.IsToggled = false;
            SetChannelAwareError("Informe o Chat ID do Telegram antes de habilitar as notificacoes.");
        }
    }

    private void OnPhoneChanged(object? sender, TextChangedEventArgs e)
    {
        var digits = new string((e.NewTextValue ?? string.Empty).Where(char.IsDigit).Take(11).ToArray());
        if (PhoneEntry.Text != digits)
            PhoneEntry.Text = digits;
    }

    private void OnPasswordChanged(object? sender, TextChangedEventArgs e)
        => UpdatePasswordRequirements();

    private bool TryBuildSaveRequest(out UserSettingsUpdateRequest? request, out string error)
    {
        request = null;
        error = string.Empty;

        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text?.Trim() ?? string.Empty;
        var confirmPassword = ConfirmPasswordEntry.Text?.Trim() ?? string.Empty;
        var phone = PhoneEntry.Text?.Trim() ?? string.Empty;
        var telegramChatId = TelegramChatIdEntry.Text?.Trim() ?? string.Empty;
        var effectiveNotifyWhatsapp = _settings?.WhatsappNotificationsEnabled == true && NotifyWhatsAppSwitch.IsToggled;
        var effectiveCalendarPublicEnabled = _settings?.CalendarIcsEnabled == true && CalendarPublicSwitch.IsToggled;

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Nome e obrigatorio.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Email e obrigatorio.";
            return false;
        }

        var wantsToChangePassword = !string.IsNullOrWhiteSpace(password) || !string.IsNullOrWhiteSpace(confirmPassword);

        if (wantsToChangePassword)
        {
            var passwordError = GetPasswordValidationMessage(password);
            if (!string.IsNullOrWhiteSpace(passwordError))
            {
                error = passwordError;
                return false;
            }

            if (password != confirmPassword)
            {
                error = "As senhas nao conferem.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(phone) && phone.Length != 11)
        {
            error = "Telefone deve ter 11 digitos no formato 99999999999.";
            return false;
        }

        if (effectiveNotifyWhatsapp && string.IsNullOrWhiteSpace(phone))
        {
            error = "Informe o telefone antes de habilitar o WhatsApp.";
            return false;
        }

        if (NotifyTelegramSwitch.IsToggled && string.IsNullOrWhiteSpace(telegramChatId))
        {
            error = "Informe o Chat ID do Telegram antes de habilitar as notificacoes.";
            return false;
        }

        request = new UserSettingsUpdateRequest(
            name,
            email,
            string.IsNullOrWhiteSpace(password) ? null : password,
            string.IsNullOrWhiteSpace(phone) ? null : phone,
            effectiveNotifyWhatsapp,
            NotifyTelegramSwitch.IsToggled,
            string.IsNullOrWhiteSpace(telegramChatId) ? null : telegramChatId,
            NotifyEmailSwitch.IsToggled,
            effectiveCalendarPublicEnabled);

        return true;
    }

    private async Task RunChannelActionAsync(Button button, string busyText, Func<Task<string>> action, Action<string> showChannelError)
    {
        ClearMessages();
        ClearChannelErrors();
        var originalText = button.Text;
        button.IsEnabled = false;
        button.Text = busyText;

        try
        {
            ShowSuccess(await action());
        }
        catch (ApiException ex)
        {
            showChannelError(ex.Message);
        }
        catch
        {
            showChannelError("Nao foi possivel concluir a operacao.");
        }
        finally
        {
            button.Text = originalText;
            button.IsEnabled = true;
        }
    }

    private void UpdatePasswordRequirements()
    {
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmation = ConfirmPasswordEntry.Text ?? string.Empty;
        var checks = GetPasswordRequirementChecks(password, confirmation);

        PasswordRequirementsBorder.IsVisible = password.Length > 0 || confirmation.Length > 0;
        ApplyRequirement(PasswordRequirement1, checks[0]);
        ApplyRequirement(PasswordRequirement2, checks[1]);
        ApplyRequirement(PasswordRequirement3, checks[2]);
        ApplyRequirement(PasswordRequirement4, checks[3]);
        ApplyRequirement(PasswordRequirement5, checks[4]);
    }

    private static void ApplyRequirement(Label label, (string Text, bool Met) requirement)
    {
        label.Text = $"{(requirement.Met ? "OK" : "X")} {requirement.Text}";
        label.TextColor = requirement.Met ? Color.FromArgb("#15803D") : Color.FromArgb("#DC2626");
    }

    private static List<(string Text, bool Met)> GetPasswordRequirementChecks(string password, string confirmPassword)
    {
        var value = password ?? string.Empty;
        var confirmation = confirmPassword ?? string.Empty;

        return
        [
            ("ao menos 9 caracteres", value.Length >= 9),
            ("ao menos uma letra", Regex.IsMatch(value, "[A-Za-z]")),
            ("ao menos 1 numero", Regex.IsMatch(value, "\\d")),
            ("ao menos um caracter especial (!@#$%*,./<>)", Regex.IsMatch(value, "[^A-Za-z0-9]")),
            ("a confirmacao da senha deve coincidir com a senha", value.Length > 0 && confirmation.Length > 0 && value == confirmation)
        ];
    }

    private static string GetPasswordValidationMessage(string password)
        => GetPasswordRequirementChecks(password, password).Take(4).All(item => item.Met)
            ? string.Empty
            : PasswordPolicyMessage;

    private void SetBusy(bool isBusy)
    {
        LoadingIndicator.IsRunning = isBusy;
        LoadingIndicator.IsVisible = isBusy;
        SaveButton.IsEnabled = !isBusy;
        VerifyEmailButton.IsEnabled = !isBusy;
        ResendEmailButton.IsEnabled = !isBusy;
        CancelEmailButton.IsEnabled = !isBusy;
        EnableTotpButton.IsEnabled = !isBusy;
        DisableTotpButton.IsEnabled = !isBusy;
    }

    private void ClearMessages()
    {
        SuccessLabel.Text = string.Empty;
        SuccessBorder.IsVisible = false;
        ErrorLabel.Text = string.Empty;
        ErrorBorder.IsVisible = false;
    }

    private void ShowSuccess(string message)
    {
        SuccessLabel.Text = message;
        SuccessBorder.IsVisible = true;
        ErrorBorder.IsVisible = false;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
        SuccessBorder.IsVisible = false;
    }

    private void ClearChannelErrors()
    {
        WhatsAppErrorBorder.IsVisible = false;
        WhatsAppErrorLabel.Text = string.Empty;
        TelegramErrorBorder.IsVisible = false;
        TelegramErrorLabel.Text = string.Empty;
        EmailErrorBorder.IsVisible = false;
        EmailErrorLabel.Text = string.Empty;
        PendingEmailErrorBorder.IsVisible = false;
        PendingEmailErrorLabel.Text = string.Empty;
    }

    private void SetChannelAwareError(string message)
    {
        message ??= string.Empty;
        var normalized = message.ToLowerInvariant();
        ClearChannelErrors();

        if (normalized.Contains("telegram"))
        {
            ShowTelegramError(message);
            return;
        }

        if (normalized.Contains("whatsapp"))
        {
            ShowWhatsAppError(message);
            return;
        }

        if (normalized.Contains("email") || normalized.Contains("e-mail"))
        {
            ShowEmailError(message);
            return;
        }

        ShowError(message);
    }

    private void ShowWhatsAppError(string message)
    {
        WhatsAppErrorLabel.Text = message;
        WhatsAppErrorBorder.IsVisible = true;
    }

    private void ShowTelegramError(string message)
    {
        TelegramErrorLabel.Text = message;
        TelegramErrorBorder.IsVisible = true;
    }

    private void ShowEmailError(string message)
    {
        EmailErrorLabel.Text = message;
        EmailErrorBorder.IsVisible = true;
    }

    private void ShowPendingEmailError(string message)
    {
        PendingEmailErrorLabel.Text = message;
        PendingEmailErrorBorder.IsVisible = true;
    }

    private void ApplyOfflineMode(bool offline)
    {
        OfflineBorder.IsVisible = offline;

        SaveButton.IsEnabled = !offline;
        NameEntry.IsEnabled = !offline;
        EmailEntry.IsEnabled = !offline;
        PasswordEntry.IsEnabled = !offline;
        ConfirmPasswordEntry.IsEnabled = !offline;
        NotifyWhatsAppSwitch.IsEnabled = !offline && NotifyWhatsAppSwitch.IsEnabled;
        NotifyTelegramSwitch.IsEnabled = !offline;
        NotifyEmailSwitch.IsEnabled = !offline;
        PhoneEntry.IsEnabled = !offline;
        TelegramChatIdEntry.IsEnabled = !offline;
        CalendarPublicSwitch.IsEnabled = !offline && CalendarPublicSwitch.IsEnabled;
        VerifyEmailButton.IsEnabled = !offline;
        ResendEmailButton.IsEnabled = !offline;
        CancelEmailButton.IsEnabled = !offline;
        TestWhatsAppButton.IsEnabled = !offline && _isAdminUser;
        TestTelegramButton.IsEnabled = !offline && _isAdminUser;
        TestEmailButton.IsEnabled = !offline && _isAdminUser;
        EnableTotpButton.IsEnabled = !offline;
        DisableTotpButton.IsEnabled = !offline;
        TotpCodeEntry.IsEnabled = !offline;
        DisableTotpCodeEntry.IsEnabled = !offline;
    }

    private void UpdateAdminOnlyTestButtons()
    {
        TestWhatsAppButton.IsVisible = _isAdminUser;
        TestTelegramButton.IsVisible = _isAdminUser;
        TestEmailButton.IsVisible = _isAdminUser;
        TestWhatsAppButton.Text = "Test WhatsApp";
        TestTelegramButton.Text = "Test Telegram";
        TestEmailButton.Text = "Test Email";
    }

    private static bool IsAdminUserType(string? userType)
        => string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(userType, "2", StringComparison.OrdinalIgnoreCase);

    private static string FormatCpf(string? cpf)
    {
        var digits = new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
            return string.Empty;

        return $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits.Substring(9, 2)}";
    }
}
