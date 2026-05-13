using System.Globalization;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class InvestmentDetailsPage : ContentPage, IQueryAttributable
{
    private const string ArchiveReinvestHint = "Voce so podera reinvestir o valor deste investimento ou arquiva-lo quando chegar a data de resgate.";

    private readonly InvestmentService _investmentService;
    private Guid? _investmentId;
    private string _investmentTitle = "este investimento";
    private InvestmentDto? _currentInvestment;

    public InvestmentDetailsPage(InvestmentService investmentService)
    {
        _investmentService = investmentService;
        InitializeComponent();
        PageFab.AddCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(AddInvestmentPage)));
        PageFab.EditCommand = new Command(async () =>
        {
            if (_investmentId.HasValue)
                await Shell.Current.GoToAsync($"{nameof(AddInvestmentPage)}?investmentId={_investmentId.Value}");
        });
        PageFab.RedeemCommand = new Command(async () => await ShowPendingActionAsync("Resgate"));
        PageFab.ReinvestCommand = new Command(async () => await ShowPendingActionAsync("Reinvestimento"));
        PageFab.ArchiveCommand = new Command(async () => await ArchiveInvestmentAsync());
        PageFab.DeleteCommand = new Command(async () => await DeleteInvestmentAsync());
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

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadInvestmentAsync();

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadInvestmentAsync();

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async void OnCurrentValueTipClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync(
            "Valor liquido atual",
            "Este e o valor liquido atual, ja descontados os impostos, considerando um resgate hoje. Para investimentos sem liquidez diaria, ele nao representa o valor do vencimento.",
            "OK");

    private async void OnIrTipClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync(
            "Imposto de Renda",
            BuildIrTip(),
            "OK");

    private async void OnIofTipClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync(
            "IOF",
            BuildIofTip(),
            "OK");

    private async Task LoadInvestmentAsync()
    {
        if (!_investmentId.HasValue)
        {
            ShowError("Investimento nao informado.");
            return;
        }

        SetLoading(true);
        HideError();

        try
        {
            var investment = await _investmentService.GetInvestmentWithCalculatedAsync(_investmentId.Value);
            _currentInvestment = investment;
            BindInvestment(investment);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar os detalhes do investimento.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void BindInvestment(InvestmentDto investment)
    {
        var currentCalc = investment.TableCalculated?.FirstOrDefault() ?? investment.Calculated?.FirstOrDefault();
        var projectionCalc = investment.DueDate.HasValue
            ? investment.TableCalculated?.Skip(1).FirstOrDefault()
              ?? investment.Calculated?.Skip(1).FirstOrDefault()
              ?? currentCalc
            : currentCalc;

        Title = investment.Title;
        _investmentTitle = investment.Title;
        TitleLabel.Text = investment.Title;
        SubtitleLabel.Text = investment.DueDate.HasValue
            ? $"Acompanhe impostos, rendimentos e valor estimado para {investment.DueDate.Value:dd/MM/yyyy}."
            : "Acompanhe impostos, rendimentos e valor liquido atual deste investimento.";

        BankLabel.Text = investment.Bank?.Name ?? "Banco";
        TypeLabel.Text = string.IsNullOrWhiteSpace(investment.InvestmentType) ? "Sem tipo" : NormalizeInvestmentType(investment.InvestmentType);
        IndexLabel.Text = BuildIndexLabel(investment);
        InvestedValueLabel.Text = MoneyFormatter.Currency(investment.PrincipalForDisplay);
        BuyDateLabel.Text = investment.DateBuy.ToLocalTime().ToString("dd/MM/yyyy");
        DueDateLabel.Text = investment.DueDate?.ToLocalTime().ToString("dd/MM/yyyy") ?? "Liquidez diaria";
        TaxesLabel.Text = investment.Taxes
            ? "Incidencia de impostos ativa para este investimento."
            : "Este investimento esta marcado como isento de IR/IOF.";

        BindCurrentValues(currentCalc, investment);
        BindProjectionValues(projectionCalc, investment);

        if (!investment.DueDate.HasValue)
        {
            InfoLabel.Text = "* Valores estimados baseados na data atual porque este investimento tem liquidez diaria.";
            InfoBorder.IsVisible = true;
        }
        else
        {
            InfoLabel.Text = string.Empty;
            InfoBorder.IsVisible = false;
        }
    }

    private void BindCurrentValues(CalculatedDto? calc, InvestmentDto investment)
    {
        CurrentGrossValueLabel.Text = MoneyFormatter.Currency(calc?.ValueBrute ?? investment.PrincipalForDisplay);
        CurrentNetValueLabel.Text = MoneyFormatter.Currency(calc?.ValueLiq ?? investment.CurrentValueForDisplay);
        CurrentProfitLabel.Text = MoneyFormatter.Currency(calc?.ProfitLiq ?? 0m);
        CurrentRateLabel.Text = FormatPercent(calc?.EffectiveIndexPercentBrute ?? 0m);

        CurrentIrValueLabel.Text = MoneyFormatter.Currency(calc?.IrValue ?? 0m);
        CurrentIrPercentLabel.Text = $"Aliquota: {FormatPercent(calc?.Ir ?? 0m)}";
        CurrentIofValueLabel.Text = MoneyFormatter.Currency(calc?.IofValue ?? 0m);
        CurrentIofPercentLabel.Text = $"Aliquota: {FormatPercent(calc?.Iof ?? 0m)}";
    }

    private void BindProjectionValues(CalculatedDto? calc, InvestmentDto investment)
    {
        var projectionTitle = investment.DueDate.HasValue ? "Projecao no vencimento" : "Estimativa com base em hoje";
        var projectionSubtitle = investment.DueDate.HasValue
            ? "Valores liquidos e impostos estimados para a data de vencimento."
            : "Como nao ha data de vencimento definida, a estimativa usa a posicao atual.";

        ProjectionTitleLabel.Text = projectionTitle;
        ProjectionSubtitleLabel.Text = projectionSubtitle;

        ProjectionGrossValueLabel.Text = MoneyFormatter.Currency(calc?.ValueBrute ?? investment.PrincipalForDisplay);
        ProjectionNetValueLabel.Text = MoneyFormatter.Currency(calc?.ValueLiq ?? investment.CurrentValueForDisplay);
        ProjectionProfitLabel.Text = MoneyFormatter.Currency(calc?.ProfitLiq ?? 0m);
        ProjectionRateLabel.Text = FormatPercent(calc?.EffectiveIndexPercentBrute ?? 0m);

        ProjectionIrValueLabel.Text = MoneyFormatter.Currency(calc?.IrValue ?? 0m);
        ProjectionIrPercentLabel.Text = $"Aliquota: {FormatPercent(calc?.Ir ?? 0m)}";
        ProjectionIofValueLabel.Text = MoneyFormatter.Currency(calc?.IofValue ?? 0m);
        ProjectionIofPercentLabel.Text = $"Aliquota: {FormatPercent(calc?.Iof ?? 0m)}";
    }

    private string BuildIrTip()
    {
        var rules = "Regras:\n- 22,5% ate 180 dias\n- 20% ate 365 dias\n- 17,5% ate 730 dias\n- 15% acima de 730 dias";

        if (BuyDateLabel.Text is not { Length: > 0 })
            return rules;

        if (!DateTime.TryParseExact(BuyDateLabel.Text, "dd/MM/yyyy", new CultureInfo("pt-BR"), DateTimeStyles.None, out var buyDate))
            return rules;

        var today = DateTime.Today;
        var elapsedDays = (today - buyDate.Date).Days;
        var thresholds = new[]
        {
            new { Days = 181, Label = "20%" },
            new { Days = 366, Label = "17,5%" },
            new { Days = 731, Label = "15%" }
        };

        var nextStep = thresholds.FirstOrDefault(item => elapsedDays < item.Days);
        if (nextStep is null)
            return rules;

        var nextDate = buyDate.Date.AddDays(nextStep.Days);
        var daysUntil = Math.Max(0, (nextDate - today).Days);
        return $"Aliquota regressiva de IR.\nProxima faixa: {nextStep.Label} em {nextDate:dd/MM/yyyy} (em {daysUntil} dias).\n\n{rules}";
    }

    private string BuildIofTip()
    {
        if (!DateTime.TryParseExact(BuyDateLabel.Text, "dd/MM/yyyy", new CultureInfo("pt-BR"), DateTimeStyles.None, out var buyDate))
        {
            return "IOF e cobrado de forma regressiva nos primeiros 30 dias. Apos 30 dias, a aliquota e zero.";
        }

        var zeroDate = buyDate.Date.AddDays(30);
        var daysUntil = Math.Max(0, (zeroDate - DateTime.Today).Days);
        return $"IOF e cobrado de forma regressiva nos primeiros 30 dias.\nApos 30 dias, a aliquota e zero.\n\nIOF zerado em: {zeroDate:dd/MM/yyyy} (em {daysUntil} dias).";
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        Refresh.IsRefreshing = false;
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

    private async Task ArchiveInvestmentAsync()
    {
        if (!_investmentId.HasValue)
            return;

        if (!CanArchiveCurrentInvestment())
        {
            await ShowArchiveUnavailableModalAsync();
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Arquivar investimento",
            $"Deseja arquivar {_investmentTitle}? Ele deixara de aparecer em Meus Investimentos por padrao.",
            "Arquivar",
            "Cancelar");

        if (!confirmed)
            return;

        SetLoading(true);
        HideError();

        try
        {
            await _investmentService.ArchiveInvestmentAsync(_investmentId.Value, archived: true);
            await DisplayAlertAsync("Investimento arquivado", $"{_investmentTitle} foi arquivado com sucesso.", "OK");
            await NavigateBackAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel arquivar o investimento.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task DeleteInvestmentAsync()
    {
        if (!_investmentId.HasValue)
            return;

        var confirmed = await DisplayAlertAsync(
            "Excluir investimento",
            $"Deseja excluir {_investmentTitle}? Esta acao nao pode ser desfeita.",
            "Excluir",
            "Cancelar");

        if (!confirmed)
            return;

        SetLoading(true);
        HideError();

        try
        {
            await _investmentService.DeleteInvestmentAsync(_investmentId.Value);
            await DisplayAlertAsync("Investimento excluido", $"{_investmentTitle} foi excluido com sucesso.", "OK");
            await NavigateBackAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel excluir o investimento.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private static string FormatPercent(decimal value)
        => $"{value.ToString(value % 1 == 0 ? "N0" : "N1", new CultureInfo("pt-BR"))}%";

    private static string NormalizeInvestmentType(string? investmentType)
        => investmentType switch
        {
            "TitulosPublicos" => "Titulos publicos",
            _ => investmentType ?? "Sem tipo"
        };

    private static string BuildIndexLabel(InvestmentDto investment)
    {
        var percent = investment.IndexPercent.ToString("N2", new CultureInfo("pt-BR"));
        return investment.Index switch
        {
            "CDI" => $"{percent}% do CDI",
            "IPCA_MAIS" => $"IPCA + {percent}%",
            "PERCENT_YEAR" => $"{percent}% a.a.",
            "CDI_MAIS" => $"CDI + {percent}% a.a.",
            _ => investment.Index
        };
    }

    private bool CanArchiveCurrentInvestment()
    {
        var dueDate = _currentInvestment?.DueDate;
        if (!dueDate.HasValue)
            return false;

        var due = dueDate.Value.ToLocalTime().Date;
        var today = DateTime.Today;
        return due <= today;
    }

    private Task ShowArchiveUnavailableModalAsync()
        => DisplayAlertAsync(
            "Arquivar investimento",
            ArchiveReinvestHint,
            "Entendi");

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

    private static Task ShowPendingActionAsync(string actionName)
        => Shell.Current?.DisplayAlertAsync(actionName, "Vamos implementar esta acao na proxima etapa.", "OK")
           ?? Task.CompletedTask;
}
