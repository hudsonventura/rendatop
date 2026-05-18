using RendaTop.App.Models;
using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class EditMoneyBoxPage : ContentPage, IQueryAttributable
{
    private readonly MoneyBoxService _service;
    private Guid? _moneyBoxId;
    private MoneyBoxDto? _currentItem;

    public EditMoneyBoxPage(MoneyBoxService service)
    {
        _service = service;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _moneyBoxId = null;
        if (query.TryGetValue("moneyBoxId", out var rawValue)
            && Guid.TryParse(rawValue?.ToString(), out var moneyBoxId))
        {
            _moneyBoxId = moneyBoxId;
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
            if (_moneyBoxId.HasValue)
            {
                var overview = await _service.GetOverviewAsync();
                _currentItem = (overview.Items ?? []).FirstOrDefault(item => item.Id == _moneyBoxId.Value);

                if (_currentItem is null)
                {
                    ShowError("Cofrinho nao encontrado.");
                    return;
                }

                HeadingLabel.Text = "Editar cofrinho";
                SaveButton.Text = "Salvar";
                NameEntry.Text = _currentItem.Name;
            }
            else
            {
                _currentItem = null;
                HeadingLabel.Text = "Novo cofrinho";
                SaveButton.Text = "Criar cofrinho";
                NameEntry.Text = string.Empty;
            }
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o cofrinho.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        HideError();
        var name = NameEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Nome do cofrinho e obrigatorio.");
            return;
        }

        SetBusy(true);

        try
        {
            if (_moneyBoxId.HasValue)
                await _service.UpdateAsync(_moneyBoxId.Value, name);
            else
                await _service.CreateAsync(name);

            await NavigateBackAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError(_moneyBoxId.HasValue
                ? "Nao foi possivel salvar o cofrinho."
                : "Nao foi possivel criar o cofrinho.");
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

    private void SetBusy(bool busy)
    {
        LoadingIndicator.IsRunning = busy;
        LoadingIndicator.IsVisible = busy;
        SaveButton.IsEnabled = !busy;
        NameEntry.IsEnabled = !busy;
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

        await shell.GoToAsync("..");
    }
}
