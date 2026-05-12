using System.Globalization;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class AddInvestmentPage : ContentPage, IQueryAttributable
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
    private Guid? _editingInvestmentId;
    private Guid? _loadedInvestmentId;

    public AddInvestmentPage(InvestmentService investmentService)
    {
        _investmentService = investmentService;
        InitializeComponent();
        PageFab.AddCommand = new Command(ActivateNewInvestmentMode);
        TypePicker.ItemsSource = TypeOptions;
        TypePicker.SelectedIndex = 0;
        IndexPicker.ItemsSource = IndexOptions;
        IndexPicker.SelectedIndex = 0;
        ResetForm();
        UpdateModeUi();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _editingInvestmentId = null;

        if (query.TryGetValue("investmentId", out var rawValue)
            && Guid.TryParse(rawValue?.ToString(), out var investmentId))
        {
            _editingInvestmentId = investmentId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadBanksAsync();
        await LoadInvestmentForEditionAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
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

    private async Task LoadInvestmentForEditionAsync()
    {
        UpdateModeUi();

        if (!_editingInvestmentId.HasValue)
        {
            if (_loadedInvestmentId.HasValue)
            {
                _loadedInvestmentId = null;
                ResetForm();
            }

            return;
        }

        if (_loadedInvestmentId == _editingInvestmentId)
            return;

        SetBusy(true);
        HideError();

        try
        {
            var investment = await _investmentService.GetInvestmentAsync(_editingInvestmentId.Value);
            FillForm(investment);
            _loadedInvestmentId = investment.Id;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o investimento para edicao.");
        }
        finally
        {
            SetBusy(false);
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
            if (_editingInvestmentId.HasValue)
            {
                await _investmentService.UpdateInvestmentAsync(_editingInvestmentId.Value, request!);
            }
            else
            {
                await _investmentService.CreateInvestmentAsync(request!);
                ResetForm();
            }

            await NavigateBackAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError(_editingInvestmentId.HasValue
                ? "Nao foi possivel atualizar o investimento."
                : "Nao foi possivel salvar o investimento.");
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

    private void FillForm(InvestmentDto investment)
    {
        TitleEntry.Text = investment.Title;
        ValueEntry.Text = investment.Value.ToString("N2", new CultureInfo("pt-BR"));
        IndexPercentEntry.Text = investment.IndexPercent.ToString("N2", new CultureInfo("pt-BR"));
        BuyDatePicker.Date = investment.DateBuy.ToLocalTime();
        DailyLiquiditySwitch.IsToggled = !investment.DueDate.HasValue;
        DueDatePicker.Date = investment.DueDate?.ToLocalTime() ?? DateTime.Today.AddYears(1);
        TaxesSwitch.IsToggled = investment.Taxes;

        SelectType(investment.InvestmentType);
        SelectIndex(investment.Index);
        SelectBank(investment.Bank?.Code);
        UpdateModeUi(investment.Title);
    }

    private void ResetForm()
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
        if (BankPicker.ItemsSource is not null)
            BankPicker.SelectedIndex = 0;
        UpdateModeUi();
    }

    private void SelectType(string? value)
    {
        var option = TypeOptions.FirstOrDefault(item => string.Equals(item.Value, value ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        TypePicker.SelectedItem = option ?? TypeOptions[0];
    }

    private void SelectIndex(string? value)
    {
        var option = IndexOptions.FirstOrDefault(item => string.Equals(item.Value, value ?? "CDI", StringComparison.OrdinalIgnoreCase));
        IndexPicker.SelectedItem = option ?? IndexOptions[0];
    }

    private void SelectBank(int? code)
    {
        if (BankPicker.ItemsSource is not IEnumerable<BankDto> banks)
            return;

        var bank = banks.FirstOrDefault(item => item.Code == code);
        if (bank is not null)
            BankPicker.SelectedItem = bank;
    }

    private void SetBusy(bool busy)
    {
        SaveButton.IsEnabled = !busy;
        SaveButton.Text = busy
            ? (_editingInvestmentId.HasValue ? "Salvando alteracoes..." : "Salvando...")
            : (_editingInvestmentId.HasValue ? "Salvar alteracoes" : "Salvar investimento");
    }

    private void UpdateModeUi(string? investmentTitle = null)
    {
        var isEditing = _editingInvestmentId.HasValue;
        Title = isEditing ? "Editar Investimento" : "Novo Investimento";
        HeadingLabel.Text = isEditing ? "Editar investimento" : "Adicionar investimento";
        DescriptionLabel.Text = isEditing
            ? string.IsNullOrWhiteSpace(investmentTitle)
                ? "Atualize os dados do investimento selecionado."
                : $"Atualize os dados de {investmentTitle}."
            : "Cadastre um investimento de renda fixa na sua carteira.";

        if (!SaveButton.IsEnabled)
            return;

        SaveButton.Text = isEditing ? "Salvar alteracoes" : "Salvar investimento";
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

    private void ActivateNewInvestmentMode()
    {
        _editingInvestmentId = null;
        _loadedInvestmentId = null;
        HideError();
        ResetForm();
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
