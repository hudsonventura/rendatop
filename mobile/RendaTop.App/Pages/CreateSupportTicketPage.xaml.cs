using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class CreateSupportTicketPage : ContentPage
{
    private readonly SupportTicketService _service;
    private readonly List<AttachmentRow> _attachments = [];

    public CreateSupportTicketPage(SupportTicketService service)
    {
        _service = service;
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () => await NavigateBackAsync());
        return true;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        HideError();

        var subject = SubjectEntry.Text?.Trim() ?? string.Empty;
        var body = MessageEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(subject))
        {
            ShowError("Assunto e obrigatorio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(body) && _attachments.Count == 0)
        {
            ShowError("A mensagem precisa ter texto ou anexo.");
            return;
        }

        SetBusy(true);

        try
        {
            await _service.CreateTicketAsync(subject, body, _attachments.Select(item => item.File).ToList());
            await DisplayAlertAsync("Chamado aberto", "Seu chamado foi enviado com sucesso.", "Ok");
            await NavigateBackAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel abrir o chamado.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async void OnPickAttachmentsClicked(object? sender, EventArgs e)
    {
        HideError();

        try
        {
            var picked = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Selecione os anexos do chamado"
            });

            if (picked is null)
                return;

            foreach (var file in picked)
            {
                if (file is null)
                    continue;

                var fullPath = file.FullPath ?? string.Empty;
                var fileName = file.FileName ?? string.Empty;

                if (_attachments.Any(item => string.Equals(item.File.FullPath ?? string.Empty, fullPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.File.FileName ?? string.Empty, fileName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!_service.IsAllowedAttachment(file))
                {
                    ShowError("Formato de anexo nao permitido. Use imagens, PDF ou arquivos Office.");
                    return;
                }

                var size = await _service.GetAttachmentSizeAsync(file);
                if (size > 1024 * 1024)
                {
                    ShowError("Cada anexo pode ter no maximo 1 MB.");
                    return;
                }

                _attachments.Add(new AttachmentRow(file, fileName, _service.FormatAttachmentSize(size)));
            }

            RefreshAttachments();
        }
        catch (Exception ex) when (ex is not ApiException)
        {
            ShowError("Nao foi possivel selecionar os anexos.");
        }
    }

    private void OnRemoveAttachmentClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not AttachmentRow row)
            return;

        _attachments.Remove(row);
        RefreshAttachments();
    }

    private void SetBusy(bool busy)
    {
        LoadingIndicator.IsRunning = busy;
        LoadingIndicator.IsVisible = busy;
        SaveButton.IsEnabled = !busy;
        SubjectEntry.IsEnabled = !busy;
        MessageEditor.IsEnabled = !busy;
        AttachmentsCollection.IsEnabled = !busy;
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

    private void RefreshAttachments()
    {
        AttachmentsCollection.ItemsSource = null;
        AttachmentsCollection.ItemsSource = _attachments.ToList();
        AttachmentsCollection.IsVisible = _attachments.Count > 0;
    }

    private sealed record AttachmentRow(FileResult File, string FileName, string SizeLabel);
}
