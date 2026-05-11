using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class MyInvestmentsPage : ContentPage
{
    private readonly InvestmentService _investmentService;

    public MyInvestmentsPage(InvestmentService investmentService)
    {
        _investmentService = investmentService;
        InitializeComponent();
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

    private async void OnAddClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//add-investment");

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

            var rows = investments.Select(InvestmentRow.FromDto).ToList();
            InvestmentsCollection.ItemsSource = rows;
            EmptyLabel.IsVisible = rows.Count == 0;
            CountLabel.Text = rows.Count.ToString();
            TotalLabel.Text = MoneyFormatter.Currency(investments.Sum(item => item.CurrentValueForDisplay));
            SubtitleLabel.Text = rows.Count == 1 ? "1 investimento ativo" : $"{rows.Count} investimentos ativos";
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
                investment.Title,
                investment.Bank?.Name ?? "Banco",
                MoneyFormatter.Currency(investment.CurrentValueForDisplay),
                index,
                dueDate);
        }
    }
}
