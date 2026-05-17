using System.Globalization;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class EditRedemptionPage : ContentPage, IQueryAttributable
{
    private readonly InvestmentService _investmentService;
    private Guid? _investmentId;
    private Guid? _redemptionId;
    private RedemptionDto? _redemption;

    public EditRedemptionPage(InvestmentService investmentService)
    {
        _investmentService = investmentService;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _investmentId = null;
        _redemptionId = null;

        if (query.TryGetValue("investmentId", out var rawInvestmentValue)
            && Guid.TryParse(rawInvestmentValue?.ToString(), out var investmentId))
        {
            _investmentId = investmentId;
        }

        if (query.TryGetValue("redemptionId", out var rawRedemptionValue)
            && Guid.TryParse(rawRedemptionValue?.ToString(), out var redemptionId))
        {
            _redemptionId = redemptionId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRedemptionAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync(_investmentId));
        return true;
    }

    private async Task LoadRedemptionAsync()
    {
        if (!_investmentId.HasValue || !_redemptionId.HasValue)
        {
            ShowError("Resgate nao informado.");
            return;
        }

        SetBusy(true);
        HideError();

        try
        {
            var investment = await _investmentService.GetInvestmentWithCalculatedAsync(_investmentId.Value);
            _redemption = investment.Redemptions?.FirstOrDefault(item => item.Id == _redemptionId.Value);

            if (_redemption is null)
            {
                ShowError("Resgate nao encontrado.");
                return;
            }

            BindRedemption(_redemption);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o resgate.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BindRedemption(RedemptionDto redemption)
    {
        TitleEntry.Text = redemption.Title;
        RedeemDatePicker.Date = redemption.Date.ToLocalTime();
        ValueEntry.Text = redemption.Value.ToString("N2", new CultureInfo("pt-BR"));
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        HideError();

        if (!TryBuildRequest(out var request, out var error))
        {
            ShowError(error);
            return;
        }

        if (!_redemptionId.HasValue)
            return;

        SetBusy(true);

        try
        {
            await _investmentService.UpdateRedemptionAsync(_redemptionId.Value, request!);
            await _investmentService.RefreshInvestmentsCacheAsync();
            await DisplayAlertAsync("Resgate atualizado", "O resgate foi atualizado com sucesso.", "OK");
            await NavigateBackAsync(_investmentId);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel atualizar o resgate.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
        => await NavigateBackAsync(_investmentId);

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync(_investmentId);

    private bool TryBuildRequest(out RedemptionRequestDto? request, out string error)
    {
        request = null;
        error = string.Empty;

        var title = TitleEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            error = "Informe a identificacao do resgate.";
            return false;
        }

        if (!TryParseDecimal(ValueEntry.Text, out var value) || value <= 0)
        {
            error = "Informe um valor maior que zero.";
            return false;
        }

        request = new RedemptionRequestDto
        {
            Title = title,
            Date = DateTime.SpecifyKind(RedeemDatePicker.Date ?? DateTime.Today, DateTimeKind.Utc),
            Value = value
        };

        return true;
    }

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        var normalized = (input ?? string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("pt-BR"), out value)
            || decimal.TryParse(normalized.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private void SetBusy(bool isBusy)
    {
        LoadingIndicator.IsVisible = isBusy;
        LoadingIndicator.IsRunning = isBusy;
        SaveButton.IsEnabled = !isBusy;
        TitleEntry.IsEnabled = !isBusy;
        RedeemDatePicker.IsEnabled = !isBusy;
        ValueEntry.IsEnabled = !isBusy;
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

    private static async Task NavigateBackAsync(Guid? investmentId)
    {
        var shell = Shell.Current;
        if (shell is null)
            return;

        if (investmentId.HasValue)
        {
            await shell.GoToAsync($"..");
            return;
        }

        await shell.GoToAsync("//meus-investimentos");
    }
}
