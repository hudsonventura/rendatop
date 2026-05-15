using System.Globalization;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class EditRecurringInvestmentPage : ContentPage, IQueryAttributable
{
    private static readonly List<int> AllMonths = Enumerable.Range(1, 12).ToList();

    private static readonly List<RecurringOption> TypeOptions =
    [
        new("Sem tipo", -1),
        new("CDB", 0),
        new("LCI", 1),
        new("LCA", 2),
        new("RCI", 3),
        new("RCA", 4),
        new("Tesouro", 5),
        new("Debentures", 6),
        new("Titulos publicos", 7),
        new("CRI", 8),
        new("CRA", 9),
        new("RDB", 10)
    ];

    private static readonly List<RecurringOption> IndexOptions =
    [
        new("CDI", 0),
        new("IPCA+", 1),
        new("% ao ano", 2),
        new("CDI + %a.a.", 3)
    ];

    private static readonly List<RecurringOption> FrequencyOptions =
    [
        new("Semanal", 0),
        new("Mensal", 1)
    ];

    private readonly RecurringInvestmentService _service;
    private readonly InvestmentService _investmentService;
    private Guid? _recurringInvestmentId;
    private bool _enabled = true;

    public EditRecurringInvestmentPage(RecurringInvestmentService service, InvestmentService investmentService)
    {
        _service = service;
        _investmentService = investmentService;
        InitializeComponent();
        TypePicker.ItemsSource = TypeOptions;
        IndexPicker.ItemsSource = IndexOptions;
        FrequencyPicker.ItemsSource = FrequencyOptions;
        TypePicker.SelectedIndex = 0;
        IndexPicker.SelectedIndex = 0;
        FrequencyPicker.SelectedIndex = 0;
        UpdateFrequencyVisibility();
        UpdateLiquidityVisibility();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _recurringInvestmentId = null;
        if (query.TryGetValue("recurringInvestmentId", out var rawValue)
            && Guid.TryParse(rawValue?.ToString(), out var recurringInvestmentId))
        {
            _recurringInvestmentId = recurringInvestmentId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
    }

    private async Task LoadAsync()
    {
        SetBusy(true);
        HideError();

        try
        {
            await LoadBanksAsync();
            var overview = await _service.GetOverviewAsync();
            _enabled = overview.RecurringInvestmentsEnabled;
            PremiumBorder.IsVisible = !_enabled;

            if (_recurringInvestmentId.HasValue)
            {
                var item = (overview.Items ?? []).FirstOrDefault(entry => entry.Id == _recurringInvestmentId.Value);
                if (item is null)
                {
                    ShowError("Recorrencia nao encontrada.");
                    return;
                }

                FillForm(item);
                HeadingLabel.Text = "Editar recorrencia";
                DescriptionLabel.Text = "Ajuste quando e como o investimento sera criado automaticamente.";
                SaveButton.Text = "Salvar alteracoes";
            }
            else
            {
                ResetForm();
                HeadingLabel.Text = "Nova recorrencia";
                DescriptionLabel.Text = "Configure quando e como o investimento sera criado automaticamente.";
                SaveButton.Text = "Criar recorrencia";
            }

            if (!_enabled)
                SaveButton.IsEnabled = false;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar a recorrencia.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadBanksAsync()
    {
        if (BankPicker.ItemsSource is not null)
            return;

        var banks = (await _investmentService.GetBanksAsync()).ToList();
        BankPicker.ItemsSource = banks;
        if (banks.Count > 0)
            BankPicker.SelectedIndex = 0;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        HideError();

        if (!_enabled)
        {
            ShowError("Investimentos recorrentes exigem um plano pago ativo.");
            return;
        }

        if (!TryBuildRequest(out var request, out var error))
        {
            ShowError(error);
            return;
        }

        SetBusy(true);

        try
        {
            var saved = _recurringInvestmentId.HasValue
                ? await _service.UpdateAsync(_recurringInvestmentId.Value, request!)
                : await _service.CreateAsync(request!);

            if (IsRecurringScheduledForToday(saved))
                await DisplayAlertAsync("Recorrencia atualizada", "Se a recorrencia era para hoje, aguarde alguns segundos e confira o investimento criado na sua carteira.", "OK");

            await NavigateBackAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError(_recurringInvestmentId.HasValue
                ? "Nao foi possivel atualizar a recorrencia."
                : "Nao foi possivel criar a recorrencia.");
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

    private void OnFrequencyChanged(object? sender, EventArgs e)
        => UpdateFrequencyVisibility();

    private void OnDailyLiquidityToggled(object? sender, ToggledEventArgs e)
        => UpdateLiquidityVisibility();

    private bool TryBuildRequest(out RecurringInvestmentRequestDto? request, out string error)
    {
        request = null;
        error = string.Empty;

        var title = TitleEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            error = "Titulo e obrigatorio.";
            return false;
        }

        if (BankPicker.SelectedItem is not BankDto bank)
        {
            error = "Selecione um banco.";
            return false;
        }

        if (!TryParseDecimal(ValueEntry.Text, out var value) || value <= 0m)
        {
            error = "Informe um valor de investimento maior que zero.";
            return false;
        }

        if (!TryParseDecimal(IndexPercentEntry.Text, out var indexPercent) || indexPercent < 0m)
        {
            error = "Informe um valor valido para o indexador.";
            return false;
        }

        var frequency = GetSelectedOption(FrequencyPicker)?.Value ?? 1;
        var liquidityDaily = DailyLiquiditySwitch.IsToggled;
        int? durationDays = null;
        if (!liquidityDaily)
        {
            if (!int.TryParse(DurationDaysEntry.Text?.Trim(), out var parsedDuration) || parsedDuration <= 0)
            {
                error = "Informe a duracao em dias quando nao houver liquidez diaria.";
                return false;
            }

            durationDays = parsedDuration;
        }

        var weekdays = frequency == 0 ? GetSelectedWeekdays() : [];
        if (frequency == 0 && weekdays.Count == 0)
        {
            error = "Selecione pelo menos um dia da semana.";
            return false;
        }

        int? dayOfMonth = null;
        var months = new List<int>();
        if (frequency == 1)
        {
            if (!int.TryParse(DayOfMonthEntry.Text?.Trim(), out var parsedDay) || parsedDay < 1 || parsedDay > 31)
            {
                error = "Informe um dia do mes entre 1 e 31.";
                return false;
            }

            months = GetSelectedMonths();
            if (months.Count == 0)
            {
                error = "Selecione pelo menos um mes.";
                return false;
            }

            dayOfMonth = parsedDay;
        }

        request = new RecurringInvestmentRequestDto
        {
            Title = title,
            InvestmentType = NormalizeType(GetSelectedOption(TypePicker)?.Value),
            BankCode = bank.Code,
            Value = value,
            Index = GetSelectedOption(IndexPicker)?.Value ?? 0,
            IndexPercent = indexPercent,
            IndexValue = 0m,
            Taxes = TaxesSwitch.IsToggled,
            LiquidityDaily = liquidityDaily,
            DurationDays = durationDays,
            Frequency = frequency,
            Weekdays = weekdays,
            DayOfMonth = dayOfMonth,
            Months = months,
            Active = ActiveSwitch.IsToggled
        };

        return true;
    }

    private void FillForm(RecurringInvestmentDto item)
    {
        TitleEntry.Text = item.Title;
        ValueEntry.Text = item.Value.ToString("N2", new CultureInfo("pt-BR"));
        IndexPercentEntry.Text = item.IndexPercent.ToString("N2", new CultureInfo("pt-BR"));
        SelectBank(item.BankCode);
        SelectOption(TypePicker, item.InvestmentTypeCode ?? -1);
        SelectOption(IndexPicker, item.IndexCode);
        SelectOption(FrequencyPicker, item.FrequencyCode);
        DailyLiquiditySwitch.IsToggled = item.LiquidityDaily;
        DurationDaysEntry.Text = item.DurationDays?.ToString() ?? string.Empty;
        TaxesSwitch.IsToggled = item.Taxes;
        ActiveSwitch.IsToggled = item.Active;
        DayOfMonthEntry.Text = item.DayOfMonth?.ToString() ?? "1";
        ApplyWeekdays(item.Weekdays ?? []);
        ApplyMonths(item.Months is { Count: > 0 } ? item.Months : AllMonths);
        UpdateFrequencyVisibility();
        UpdateLiquidityVisibility();
    }

    private void ResetForm()
    {
        TitleEntry.Text = string.Empty;
        ValueEntry.Text = string.Empty;
        IndexPercentEntry.Text = string.Empty;
        TypePicker.SelectedIndex = 0;
        IndexPicker.SelectedIndex = 0;
        FrequencyPicker.SelectedIndex = 0;
        DailyLiquiditySwitch.IsToggled = false;
        DurationDaysEntry.Text = string.Empty;
        TaxesSwitch.IsToggled = true;
        ActiveSwitch.IsToggled = true;
        DayOfMonthEntry.Text = "1";
        ApplyWeekdays([]);
        ApplyMonths(AllMonths);
        UpdateFrequencyVisibility();
        UpdateLiquidityVisibility();
    }

    private void UpdateFrequencyVisibility()
    {
        var frequency = GetSelectedOption(FrequencyPicker)?.Value ?? 0;
        WeeklyBorder.IsVisible = frequency == 0;
        MonthlyBorder.IsVisible = frequency == 1;
    }

    private void UpdateLiquidityVisibility()
    {
        DurationDaysEntry.IsEnabled = !DailyLiquiditySwitch.IsToggled;
        DurationDaysEntry.Opacity = DailyLiquiditySwitch.IsToggled ? 0.45 : 1;
    }

    private void SetBusy(bool busy)
    {
        LoadingIndicator.IsRunning = busy;
        LoadingIndicator.IsVisible = busy;
        SaveButton.IsEnabled = !busy && _enabled;
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

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        var normalized = (input ?? string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("pt-BR"), out value)
            || decimal.TryParse(normalized.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static int? NormalizeType(int? value) => value is null or < 0 ? null : value;

    private static RecurringOption? GetSelectedOption(Picker picker)
        => picker.SelectedItem as RecurringOption;

    private static void SelectOption(Picker picker, int value)
    {
        if (picker.ItemsSource is not IEnumerable<RecurringOption> options)
            return;

        var target = options.FirstOrDefault(option => option.Value == value);
        if (target is not null)
            picker.SelectedItem = target;
    }

    private void SelectBank(int bankCode)
    {
        if (BankPicker.ItemsSource is not IEnumerable<BankDto> banks)
            return;

        var bank = banks.FirstOrDefault(item => item.Code == bankCode);
        if (bank is not null)
            BankPicker.SelectedItem = bank;
    }

    private List<short> GetSelectedWeekdays()
    {
        var result = new List<short>();
        if (SundayCheck.IsChecked) result.Add(0);
        if (MondayCheck.IsChecked) result.Add(1);
        if (TuesdayCheck.IsChecked) result.Add(2);
        if (WednesdayCheck.IsChecked) result.Add(3);
        if (ThursdayCheck.IsChecked) result.Add(4);
        if (FridayCheck.IsChecked) result.Add(5);
        if (SaturdayCheck.IsChecked) result.Add(6);
        return result;
    }

    private void ApplyWeekdays(IEnumerable<short> weekdays)
    {
        var set = weekdays.ToHashSet();
        SundayCheck.IsChecked = set.Contains(0);
        MondayCheck.IsChecked = set.Contains(1);
        TuesdayCheck.IsChecked = set.Contains(2);
        WednesdayCheck.IsChecked = set.Contains(3);
        ThursdayCheck.IsChecked = set.Contains(4);
        FridayCheck.IsChecked = set.Contains(5);
        SaturdayCheck.IsChecked = set.Contains(6);
    }

    private List<int> GetSelectedMonths()
    {
        var result = new List<int>();
        if (JanuaryCheck.IsChecked) result.Add(1);
        if (FebruaryCheck.IsChecked) result.Add(2);
        if (MarchCheck.IsChecked) result.Add(3);
        if (AprilCheck.IsChecked) result.Add(4);
        if (MayCheck.IsChecked) result.Add(5);
        if (JuneCheck.IsChecked) result.Add(6);
        if (JulyCheck.IsChecked) result.Add(7);
        if (AugustCheck.IsChecked) result.Add(8);
        if (SeptemberCheck.IsChecked) result.Add(9);
        if (OctoberCheck.IsChecked) result.Add(10);
        if (NovemberCheck.IsChecked) result.Add(11);
        if (DecemberCheck.IsChecked) result.Add(12);
        return result;
    }

    private void ApplyMonths(IEnumerable<int> months)
    {
        var set = months.ToHashSet();
        JanuaryCheck.IsChecked = set.Contains(1);
        FebruaryCheck.IsChecked = set.Contains(2);
        MarchCheck.IsChecked = set.Contains(3);
        AprilCheck.IsChecked = set.Contains(4);
        MayCheck.IsChecked = set.Contains(5);
        JuneCheck.IsChecked = set.Contains(6);
        JulyCheck.IsChecked = set.Contains(7);
        AugustCheck.IsChecked = set.Contains(8);
        SeptemberCheck.IsChecked = set.Contains(9);
        OctoberCheck.IsChecked = set.Contains(10);
        NovemberCheck.IsChecked = set.Contains(11);
        DecemberCheck.IsChecked = set.Contains(12);
    }

    private static bool IsRecurringScheduledForToday(RecurringInvestmentDto item)
    {
        if (!item.Active)
            return false;

        var today = DateTime.Today;
        if (item.FrequencyCode == 0)
            return (item.Weekdays ?? []).Contains((short)today.DayOfWeek);

        var months = item.Months ?? [];
        if (!months.Contains(today.Month))
            return false;

        var requestedDay = item.DayOfMonth ?? 1;
        var lastDay = DateTime.DaysInMonth(today.Year, today.Month);
        return today.Day == Math.Min(requestedDay, lastDay);
    }

    private static async Task NavigateBackAsync()
    {
        var shell = Shell.Current;
        if (shell is null)
            return;

        await shell.GoToAsync("..");
    }
}
