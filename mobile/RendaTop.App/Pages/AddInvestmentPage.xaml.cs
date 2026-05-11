using System.Globalization;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class AddInvestmentPage : ContentPage
{
    private static readonly List<InvestmentOption> TypeOptions =
    [
        new("Sem tipo", string.Empty),
        new("CDB", "CDB"),
        new("RDB", "RDB"),
        new("LCI", "LCI"),
        new("LCA", "LCA"),
        new("RCI", "RCI"),
        new("RCA", "RCA"),
        new("Tesouro", "Tesouro"),
        new("Debentures", "Debentures"),
        new("Titulos publicos", "TitulosPublicos"),
        new("CRI", "CRI"),
        new("CRA", "CRA")
    ];

    private static readonly List<InvestmentIndexOption> IndexOptions =
    [
        new("CDI", "CDI"),
        new("IPCA+", "IPCA_MAIS"),
        new("% a.a.", "PERCENT_YEAR"),
        new("CDI + % a.a.", "CDI_MAIS")
    ];

    private readonly InvestmentService _investmentService;

    public AddInvestmentPage(InvestmentService investmentService)
    {
        _investmentService = investmentService;
        InitializeComponent();
        TypePicker.ItemsSource = TypeOptions;
        TypePicker.SelectedIndex = 0;
        IndexPicker.ItemsSource = IndexOptions;
        IndexPicker.SelectedIndex = 0;
        BuyDatePicker.Date = DateTime.Today;
        DueDatePicker.Date = DateTime.Today.AddYears(1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadBanksAsync();
    }

    private async Task LoadBanksAsync()
    {
        if (BankPicker.ItemsSource is not null)
            return;

        try
        {
            var banks = (await _investmentService.GetBanksAsync()).ToList();
            BankPicker.ItemsSource = banks;
            if (banks.Count > 0)
                BankPicker.SelectedIndex = 0;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar a lista de bancos.");
        }
    }

    private void OnDailyLiquidityToggled(object? sender, ToggledEventArgs e)
    {
        DueDatePicker.IsEnabled = !e.Value;
        DueDatePicker.Opacity = e.Value ? 0.45 : 1;
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
            await _investmentService.CreateInvestmentAsync(request!);
            ClearForm();
            await Shell.Current.GoToAsync("//meus-investimentos");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel salvar o investimento.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//meus-investimentos");

    private bool TryBuildRequest(out InvestmentRequestDto? request, out string error)
    {
        request = null;
        error = string.Empty;

        var title = TitleEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            error = "Informe o titulo.";
            return false;
        }

        if (BankPicker.SelectedItem is not BankDto bank)
        {
            error = "Selecione o banco.";
            return false;
        }

        if (!TryParseDecimal(ValueEntry.Text, out var value) || value <= 0)
        {
            error = "Informe um valor investido valido.";
            return false;
        }

        if (IndexPicker.SelectedItem is not InvestmentIndexOption index)
        {
            error = "Selecione o indexador.";
            return false;
        }

        if (!TryParseDecimal(IndexPercentEntry.Text, out var indexPercent) || indexPercent <= 0)
        {
            error = "Informe o percentual do indexador.";
            return false;
        }

        var type = TypePicker.SelectedItem as InvestmentOption;
        request = new InvestmentRequestDto
        {
            Title = title,
            InvestmentType = string.IsNullOrWhiteSpace(type?.Value) ? null : type.Value,
            BankCode = bank.Code,
            DateBuy = DateTime.SpecifyKind(BuyDatePicker.Date ?? DateTime.Today, DateTimeKind.Utc),
            DateExpectedSell = DailyLiquiditySwitch.IsToggled
                ? null
                : DateTime.SpecifyKind(DueDatePicker.Date ?? DateTime.Today.AddYears(1), DateTimeKind.Utc),
            Value = value,
            Index = index.Value,
            IndexPercent = indexPercent,
            IndexValue = 0m,
            Taxes = TaxesSwitch.IsToggled,
            Archived = false,
            AiExtracted = false
        };
        return true;
    }

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        var normalized = (input ?? string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("pt-BR"), out value)
            || decimal.TryParse(normalized.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private void ClearForm()
    {
        TitleEntry.Text = string.Empty;
        ValueEntry.Text = string.Empty;
        IndexPercentEntry.Text = string.Empty;
        TypePicker.SelectedIndex = 0;
        IndexPicker.SelectedIndex = 0;
        TaxesSwitch.IsToggled = true;
        DailyLiquiditySwitch.IsToggled = false;
        BuyDatePicker.Date = DateTime.Today;
        DueDatePicker.Date = DateTime.Today.AddYears(1);
    }

    private void SetBusy(bool busy)
    {
        SaveButton.IsEnabled = !busy;
        SaveButton.Text = busy ? "Salvando..." : "Salvar investimento";
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
