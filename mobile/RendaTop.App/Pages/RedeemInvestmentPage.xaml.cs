using System.Globalization;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class RedeemInvestmentPage : ContentPage, IQueryAttributable
{
    private readonly InvestmentService _investmentService;
    private Guid? _investmentId;
    private InvestmentDto? _investment;

    public RedeemInvestmentPage(InvestmentService investmentService)
    {
        _investmentService = investmentService;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _investmentId = null;
        if (query.TryGetValue("investmentId", out var rawValue)
            && Guid.TryParse(rawValue?.ToString(), out var investmentId))
        {
            _investmentId = investmentId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInvestmentAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
    }

    private async Task LoadInvestmentAsync()
    {
        if (!_investmentId.HasValue)
        {
            ShowError("Investimento nao informado.");
            return;
        }

        SetBusy(true);
        HideError();

        try
        {
            _investment = await _investmentService.GetInvestmentWithCalculatedAsync(_investmentId.Value);
            BindInvestment(_investment);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar os dados do investimento para resgate.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BindInvestment(InvestmentDto investment)
    {
        Title = "Criar resgate";
        HeadingLabel.Text = "Criar resgate";
        DescriptionLabel.Text = $"Informe os dados do resgate para o investimento {investment.Title}.";
        CurrentValueLabel.Text = MoneyFormatter.Currency(investment.CurrentValueForDisplay);
        TitleEntry.Text = $"Resgate - {investment.Title}";
        RedeemDatePicker.Date = DateTime.Today;
        ValueEntry.Text = string.Empty;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        HideError();

        if (!TryBuildRequest(out var request, out var error))
        {
            ShowError(error);
            return;
        }

        SetBusy(true);

        try
        {
            await _investmentService.RedeemInvestmentAsync(_investmentId!.Value, request!);

            try
            {
                await _investmentService.RefreshInvestmentsCacheAsync();
            }
            catch
            {
                // O backend ja confirmou o resgate; se o cache falhar, a proxima sincronizacao corrige.
            }

            await DisplayAlertAsync("Resgate confirmado", "O resgate foi registrado com sucesso.", "OK");
            await NavigateBackAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel registrar o resgate.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private void OnFullRedeemClicked(object? sender, EventArgs e)
    {
        if (_investment is null)
            return;

        ValueEntry.Text = _investment.CurrentValueForDisplay.ToString("N2", new CultureInfo("pt-BR"));
    }

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

    private static async Task NavigateBackAsync()
    {
        var shell = Shell.Current;
        if (shell is null)
            return;

        var navigation = shell.Navigation;
        if (navigation?.NavigationStack?.Count > 1)
        {
            await shell.GoToAsync("..");
            return;
        }

        await shell.GoToAsync("//meus-investimentos");
    }
}
