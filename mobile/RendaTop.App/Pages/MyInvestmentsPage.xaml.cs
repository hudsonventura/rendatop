using System.Collections.ObjectModel;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class MyInvestmentsPage : ContentPage
{
    private readonly InvestmentService _investmentService;
    private readonly ObservableCollection<InvestmentRow> _rows = [];

    public MyInvestmentsPage(InvestmentService investmentService)
    {
        _investmentService = investmentService;
        InitializeComponent();
        InvestmentsCollection.ItemsSource = _rows;
        PageFab.AddCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(AddInvestmentPage)));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInvestmentsAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadInvestmentsAsync();

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadInvestmentsAsync();

    private async Task LoadInvestmentsAsync()
    {
        SetLoading(true);
        HideError();

        try
        {
            var investments = (await _investmentService.GetInvestmentsAsync())
                .Where(item => !item.Archived)
                .OrderBy(item => item.DueDate ?? DateTime.MaxValue)
                .ToList();

            _rows.Clear();
            foreach (var row in investments.Select(InvestmentRow.FromDto))
                _rows.Add(row);

            InvestmentsCollection.SelectedItem = null;
            EmptyLabel.IsVisible = _rows.Count == 0;
            CountLabel.Text = _rows.Count.ToString();
            TotalLabel.Text = MoneyFormatter.Currency(investments.Sum(item => item.CurrentValueForDisplay));
            SubtitleLabel.Text = _rows.Count == 1 ? "1 investimento ativo" : $"{_rows.Count} investimentos ativos";
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar seus investimentos.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not InvestmentRow row)
            return;

        InvestmentsCollection.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(InvestmentDetailsPage)}?investmentId={row.Id}");
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

    private sealed record InvestmentRow(
        Guid Id,
        string Title,
        string BankName,
        string CurrentValue,
        string IndexLabel,
        string DueDateLabel)
    {
        public static InvestmentRow FromDto(InvestmentDto investment)
        {
            var dueDate = investment.DueDate?.ToString("dd/MM/yyyy") ?? "Liquidez diaria";
            var index = investment.Calculated?.FirstOrDefault()?.ProfitLiq is not null
                ? investment.CurrentValueForDisplay >= investment.PrincipalForDisplay
                    ? "Rendimento atualizado"
                    : "Valor atualizado"
                : "Investimento";

            return new InvestmentRow(
                investment.Id,
                investment.Title,
                investment.Bank?.Name ?? "Banco",
                MoneyFormatter.Currency(investment.CurrentValueForDisplay),
                index,
                dueDate);
        }
    }
}
