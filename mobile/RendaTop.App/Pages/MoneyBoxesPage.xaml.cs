using RendaTop.App.Controls;
using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class MoneyBoxesPage : ContentPage
{
    private readonly MoneyBoxService _service;
    private readonly ConnectivityService _connectivity;
    private readonly NotificationTitleView _titleView;
    private bool _canCreate;

    public MoneyBoxesPage(MoneyBoxService service, ConnectivityService connectivity, NotificationService notifications)
    {
        _service = service;
        _connectivity = connectivity;
        InitializeComponent();
        _titleView = NotificationChrome.Apply(this, "Cofrinhos", notifications);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _titleView.RefreshAsync();
        await LoadAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnCreateClicked(object? sender, EventArgs e)
        => await OpenCreateAsync();

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: MoneyBoxRow row })
            await Shell.Current.GoToAsync($"{nameof(EditMoneyBoxPage)}?moneyBoxId={row.Id}");
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: MoneyBoxRow row })
            return;

        var confirmed = await DisplayAlertAsync(
            "Excluir cofrinho?",
            "Ao excluir este cofrinho, os investimentos vinculados continuam existindo e apenas perdem esse vinculo.",
            "Excluir",
            "Cancelar");

        if (!confirmed)
            return;

        SetLoading(true);
        HideError();

        try
        {
            await _service.DeleteAsync(row.Id);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel excluir o cofrinho.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task OpenCreateAsync()
    {
        if (!_canCreate)
        {
            var message = RestrictionLabel.Text;
            await DisplayAlertAsync("Regras do plano", string.IsNullOrWhiteSpace(message) ? "Nao e possivel criar novos cofrinhos no momento." : message, "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(EditMoneyBoxPage));
    }

    private async Task LoadAsync()
    {
        SetLoading(true);
        HideError();

        try
        {
            var offline = _connectivity.IsOffline;
            var overview = offline
                ? await _service.GetCachedOverviewAsync()
                : await _service.GetOverviewAsync();

            _canCreate = overview.CanCreate;
            CreateButton.IsEnabled = overview.CanCreate && !offline;
            OfflineBorder.IsVisible = offline;
            RestrictionBorder.IsVisible = !string.IsNullOrWhiteSpace(overview.RestrictionMessage);
            RestrictionLabel.Text = overview.RestrictionMessage ?? string.Empty;

            CountLabel.Text = overview.Limit.HasValue
                ? $"{overview.Count}/{overview.Limit.Value}"
                : $"{overview.Count} ilimitado";

            var rows = (overview.Items ?? [])
                .OrderBy(item => item.Name)
                .Select(item => MoneyBoxRow.FromDto(item, !offline))
                .ToList();

            ItemsCollection.ItemsSource = rows;
            EmptyLabel.IsVisible = rows.Count == 0;
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar seus cofrinhos.");
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

    private sealed record MoneyBoxRow(Guid Id, string Name, string CreatedAtLabel, string TotalLiquidValueLabel, bool CanWrite)
    {
        public static MoneyBoxRow FromDto(MoneyBoxDto item, bool canWrite) =>
            new(
                item.Id,
                item.Name,
                $"Criado em {item.CreatedAt.ToLocalTime():dd/MM/yyyy}",
                MoneyFormatter.Currency(item.TotalLiquidValue),
                canWrite);
    }
}
