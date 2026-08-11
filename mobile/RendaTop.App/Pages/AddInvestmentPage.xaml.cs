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
    private readonly SharedInvestmentDocumentService _sharedDocuments;
    private Guid? _editingInvestmentId;
    private Guid? _loadedInvestmentId;
    private Guid? _reinvestSourceInvestmentId;
    private bool _isReinvesting;
    private bool _wasAiExtracted;

    private Guid? _sharedDocumentId;
    private Guid? _processedSharedDocumentId;

    public AddInvestmentPage(InvestmentService investmentService, SharedInvestmentDocumentService sharedDocuments)
    {
        _investmentService = investmentService;
		_sharedDocuments = sharedDocuments;
        InitializeComponent();
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
        _reinvestSourceInvestmentId = null;
        _isReinvesting = false;

        if (query.TryGetValue("investmentId", out var rawValue)
            && Guid.TryParse(rawValue?.ToString(), out var investmentId))
        {
            _editingInvestmentId = investmentId;
        }

        if (query.TryGetValue("reinvestSourceInvestmentId", out var rawReinvestValue)
            && Guid.TryParse(rawReinvestValue?.ToString(), out var reinvestSourceId))
        {
            _reinvestSourceInvestmentId = reinvestSourceId;
            _isReinvesting = true;
        }

        _sharedDocumentId = query.TryGetValue("sharedDocumentId", out var rawSharedDocumentId)
            && Guid.TryParse(rawSharedDocumentId?.ToString(), out var sharedDocumentId)
            ? sharedDocumentId
            : null;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadBanksAsync();
        await LoadInvestmentForEditionAsync();
        await LoadInvestmentForReinvestmentAsync();
        await ProcessSharedDocumentAsync();
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

    private async Task LoadInvestmentForReinvestmentAsync()
    {
        if (!_reinvestSourceInvestmentId.HasValue || _editingInvestmentId.HasValue)
            return;

        if (_loadedInvestmentId == _reinvestSourceInvestmentId)
            return;

        SetBusy(true);
        HideError();

        try
        {
            var investment = await _investmentService.GetInvestmentWithCalculatedAsync(_reinvestSourceInvestmentId.Value);
            FillForm(investment);
            _loadedInvestmentId = investment.Id;
            UpdateModeUi(investment.Title);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o investimento para reinvestimento.");
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

    private async void OnDatePickerDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (sender is not DatePicker picker)
            return;

        await Task.Delay(100);
        picker.Unfocus();
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
                await _investmentService.RefreshInvestmentsCacheAsync();
            }
            else
            {
                var createdId = await _investmentService.CreateInvestmentAsync(request!);
                await _investmentService.UpsertInvestmentInCacheAsync(BuildCreatedInvestment(createdId, request!));

                if (_reinvestSourceInvestmentId.HasValue)
                {
                    try
                    {
                        await _investmentService.ArchiveInvestmentAsync(_reinvestSourceInvestmentId.Value, archived: true);
                        await _investmentService.ArchiveInvestmentInCacheAsync(_reinvestSourceInvestmentId.Value, archived: true);
                    }
                    catch (ApiException ex)
                    {
                        await _investmentService.RefreshInvestmentsCacheAsync();
                        ShowError($"Novo investimento criado, mas nao foi possivel arquivar o original. {ex.Message}");
                        return;
                    }
                    catch
                    {
                        await _investmentService.RefreshInvestmentsCacheAsync();
                        ShowError("Novo investimento criado, mas nao foi possivel arquivar o investimento original.");
                        return;
                    }
                }

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

    private async void OnExtractDocumentClicked(object? sender, EventArgs e)
    {
        HideError();

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione o comprovante do investimento"
            });

            if (file is null)
                return;

            if (!IsSupportedDocument(file.FileName))
            {
                ShowError("Formato nao suportado. Envie txt, html, PDF ou imagem.");
                return;
            }

            await ExtractDocumentAsync(file);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel ler o arquivo. Tente novamente.");
        }
    }

    private async Task ProcessSharedDocumentAsync()
    {
        if (!_sharedDocumentId.HasValue || _processedSharedDocumentId == _sharedDocumentId)
            return;

        _processedSharedDocumentId = _sharedDocumentId;
        if (!_sharedDocuments.TryTake(_sharedDocumentId.Value, out var document) || document is null)
            return;

        try
        {
            if (!IsSupportedDocument(document.FileName))
            {
                ShowError("Formato compartilhado nao suportado. Envie txt, html, PDF ou imagem.");
                return;
            }

            await ExtractDocumentAsync(new FileResult(document.FilePath, document.ContentType ?? "application/octet-stream"));
        }
        finally
        {
            _sharedDocuments.DeleteCachedFile(document);
        }
    }

    private async Task ExtractDocumentAsync(FileResult file)
    {
        ExtractDocumentButton.IsEnabled = false;
        ExtractDocumentButton.Text = "Lendo...";

        try
        {
            var extracted = await _investmentService.ExtractInvestmentFromDocumentAsync(file);
            ApplyExtractedValues(extracted);
            _wasAiExtracted = true;

            await DisplayAlertAsync(
                "Campos preenchidos",
                string.IsNullOrWhiteSpace(extracted.Notes)
                    ? "A IA preencheu os campos encontrados no comprovante. Revise-os antes de salvar."
                    : extracted.Notes,
                "Ok");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel ler o arquivo. Tente novamente.");
        }
        finally
        {
            ExtractDocumentButton.IsEnabled = true;
            ExtractDocumentButton.Text = "Ler arquivo";
        }
    }

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
            AiExtracted = _wasAiExtracted && !_editingInvestmentId.HasValue
        };
        return true;
    }

    private void ApplyExtractedValues(InvestmentDocumentExtractionDto extracted)
    {
        if (!string.IsNullOrWhiteSpace(extracted.Title))
            TitleEntry.Text = extracted.Title;

        if (extracted.DateBuy.HasValue)
            BuyDatePicker.Date = extracted.DateBuy.Value.ToLocalTime();

        if (extracted.DueDate.HasValue)
            DueDatePicker.Date = extracted.DueDate.Value.ToLocalTime();

        if (extracted.DailyLiquidity.HasValue)
            DailyLiquiditySwitch.IsToggled = extracted.DailyLiquidity.Value;

        if (extracted.Taxes.HasValue)
            TaxesSwitch.IsToggled = extracted.Taxes.Value;

        if (extracted.Value.HasValue)
            ValueEntry.Text = extracted.Value.Value.ToString("N2", new CultureInfo("pt-BR"));

        if (extracted.IndexPercent.HasValue)
            IndexPercentEntry.Text = extracted.IndexPercent.Value.ToString("N2", new CultureInfo("pt-BR"));

        if (extracted.InvestmentType.HasValue)
            SelectType(MapInvestmentType(extracted.InvestmentType.Value));

        if (extracted.Index.HasValue)
            SelectIndex(MapIndex(extracted.Index.Value));

        if (extracted.BankCode.HasValue)
            SelectBank(extracted.BankCode.Value);
    }

    private static bool IsSupportedDocument(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string? MapInvestmentType(int value) => value switch
    {
        0 => "CDB",
        1 => "LCI",
        2 => "LCA",
        3 => "RCI",
        4 => "RCA",
        5 => "Tesouro",
        6 => "Debentures",
        7 => "TitulosPublicos",
        8 => "CRI",
        9 => "CRA",
        10 => "RDB",
        _ => null
    };

    private static string? MapIndex(int value) => value switch
    {
        0 => "CDI",
        1 => "IPCA_MAIS",
        2 => "PERCENT_YEAR",
        3 => "CDI_MAIS",
        _ => null
    };

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
        _wasAiExtracted = false;
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
            ? (_editingInvestmentId.HasValue ? "Salvando alteracoes..." : _isReinvesting ? "Confirmando reinvestimento..." : "Salvando...")
            : (_editingInvestmentId.HasValue ? "Salvar alteracoes" : _isReinvesting ? "Confirmar reinvestimento" : "Salvar investimento");
    }

    private void UpdateModeUi(string? investmentTitle = null)
    {
        var isEditing = _editingInvestmentId.HasValue;
        var isReinvesting = _isReinvesting && !isEditing;
        Title = isEditing ? "Editar Investimento" : isReinvesting ? "Reinvestir Investimento" : "Novo Investimento";
        HeadingLabel.Text = isEditing ? "Editar investimento" : isReinvesting ? "Reinvestir investimento" : "Adicionar investimento";
        DescriptionLabel.Text = isEditing
            ? string.IsNullOrWhiteSpace(investmentTitle)
                ? "Atualize os dados do investimento selecionado."
                : $"Atualize os dados de {investmentTitle}."
            : isReinvesting
                ? string.IsNullOrWhiteSpace(investmentTitle)
                    ? "Revise os dados para criar o novo investimento e arquivar o anterior."
                    : $"Revise os dados de {investmentTitle} para criar o novo investimento e arquivar o anterior."
                : "Cadastre um investimento de renda fixa na sua carteira.";

        if (!SaveButton.IsEnabled)
            return;

        SaveButton.Text = isEditing ? "Salvar alteracoes" : isReinvesting ? "Confirmar reinvestimento" : "Salvar investimento";
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
        _reinvestSourceInvestmentId = null;
        _isReinvesting = false;
        HideError();
        ResetForm();
    }

    private InvestmentDto BuildCreatedInvestment(Guid investmentId, InvestmentRequestDto request)
    {
        var bank = BankPicker.SelectedItem as BankDto;

        return new InvestmentDto
        {
            Id = investmentId,
            Title = request.Title,
            DateBuy = request.DateBuy,
            DueDate = request.DateExpectedSell,
            Value = request.Value,
            InvestmentType = request.InvestmentType,
            MoneyBoxId = request.MoneyBoxId,
            Index = request.Index,
            IndexPercent = request.IndexPercent,
            IndexValue = request.IndexValue,
            Taxes = request.Taxes,
            TableValue = request.Value,
            Archived = request.Archived,
            Bank = bank,
            Calculated = [],
            TableCalculated = []
        };
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
