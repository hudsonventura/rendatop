using RendaTop.App.Services;

namespace RendaTop.App.Pages;

[QueryProperty(nameof(TicketId), "ticketId")]
public partial class SupportTicketDetailsPage : ContentPage
{
    private readonly SupportTicketService _service;
    private readonly ConnectivityService _connectivity;
    private readonly List<ReplyAttachmentRow> _replyAttachments = [];
    private Guid _ticketId;

    public SupportTicketDetailsPage(SupportTicketService service, ConnectivityService connectivity)
    {
        _service = service;
        _connectivity = connectivity;
        InitializeComponent();
    }

    public string TicketId
    {
        set
        {
            if (Guid.TryParse(value, out var parsed))
                _ticketId = parsed;
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
        SetLoading(true);
        HideError();

        try
        {
            OfflineBorder.IsVisible = _connectivity.IsOffline;
            var detail = _connectivity.IsOffline
                ? await _service.GetCachedTicketAsync(_ticketId)
                : await _service.GetTicketAsync(_ticketId);

            if (detail is null)
                throw new ApiException("Este chamado ainda nao esta disponivel offline. Abra-o uma vez com internet para guardar os detalhes.", 404);

            ApplyDetail(detail);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch
        {
            ShowError("Nao foi possivel carregar o chamado.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await LoadAsync();

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigateBackAsync();

    private async void OnReplyClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowReplyError("Sem conexao. O chamado fica em leitura no modo offline.");
            return;
        }

        HideReplyError();

        var body = ReplyEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(body) && _replyAttachments.Count == 0)
        {
            ShowReplyError("A resposta precisa ter texto ou anexo.");
            return;
        }

        SetBusy(true);

        try
        {
            var detail = await _service.AddMessageAsync(_ticketId, body, _replyAttachments.Select(item => item.File).ToList());
            ApplyDetail(detail);
            ReplyEditor.Text = string.Empty;
            _replyAttachments.Clear();
            RefreshReplyAttachments();
            await DisplayAlertAsync("Resposta enviada", "Sua resposta foi registrada com sucesso.", "Ok");
        }
        catch (ApiException ex)
        {
            ShowReplyError(ex.Message);
        }
        catch
        {
            ShowReplyError("Nao foi possivel enviar a resposta.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnPickAttachmentsClicked(object? sender, EventArgs e)
    {
        if (_connectivity.IsOffline)
        {
            ShowReplyError("Sem conexao. O chamado fica em leitura no modo offline.");
            return;
        }

        HideReplyError();

        try
        {
            var picked = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Selecione os anexos da resposta"
            });

            if (picked is null)
                return;

            foreach (var file in picked)
            {
                if (file is null)
                    continue;

                var fullPath = file.FullPath ?? string.Empty;
                var fileName = file.FileName ?? string.Empty;

                if (_replyAttachments.Any(item =>
                        string.Equals(item.File.FullPath ?? string.Empty, fullPath, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(item.File.FileName ?? string.Empty, fileName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!_service.IsAllowedAttachment(file))
                {
                    ShowReplyError("Formato de anexo nao permitido. Use imagens, PDF ou arquivos Office.");
                    return;
                }

                var size = await _service.GetAttachmentSizeAsync(file);
                if (size > 1024 * 1024)
                {
                    ShowReplyError("Cada anexo pode ter no maximo 1 MB.");
                    return;
                }

                _replyAttachments.Add(new ReplyAttachmentRow(file, fileName, FormatAttachmentSize(size)));
            }

            RefreshReplyAttachments();
        }
        catch
        {
            ShowReplyError("Nao foi possivel selecionar os anexos.");
        }
    }

    private void OnRemoveAttachmentClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not ReplyAttachmentRow row)
            return;

        _replyAttachments.Remove(row);
        RefreshReplyAttachments();
    }

    private void ApplyDetail(Models.SupportTicketDetailDto detail)
    {
        SubjectLabel.Text = detail.Subject;
        StatusLabel.Text = GetStatusLabel(detail.Status);
        RequesterLabel.Text = $"{detail.RequesterUserName} ({detail.RequesterUserEmail})";
        UpdatedAtLabel.Text = detail.UpdatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        PendingLabel.Text = detail.PendingFor switch
        {
            "admin" => "Aguardando atendimento do time",
            "user" => "Aguardando retorno do usuario",
            _ => detail.IsArchived ? "Chamado arquivado" : "Chamado em acompanhamento"
        };
        TicketMetaLabel.Text = $"Criado em {detail.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}";
        ReplyBorder.IsVisible = detail.CanCurrentUserReply && !_connectivity.IsOffline;

        var (bg, stroke, text) = detail.Status switch
        {
            Models.SupportStatus.AguardandoAtendimento => Palette("#FEF3C7", "#FCD34D", "#92400E"),
            Models.SupportStatus.EmAtendimento => Palette("#DBEAFE", "#93C5FD", "#1D4ED8"),
            Models.SupportStatus.AguardandoRespostaUsuario => Palette("#EDE9FE", "#C4B5FD", "#6D28D9"),
            Models.SupportStatus.Encerrado => Palette("#DCFCE7", "#86EFAC", "#166534"),
            Models.SupportStatus.Cancelado => Palette("#E5E7EB", "#D1D5DB", "#374151"),
            _ => Palette("#F1F5F9", "#CBD5E1", "#334155")
        };

        StatusBorder.BackgroundColor = bg;
        StatusBorder.Stroke = stroke;
        StatusLabel.TextColor = text;

        var rows = (detail.Messages ?? [])
            .OrderBy(item => item.CreatedAt)
            .Select(message => new MessageRow(
                message.SenderUserName,
                GetSenderTypeLabel(message.SenderUserType),
                string.IsNullOrWhiteSpace(message.BodyText) ? "Mensagem sem texto." : message.BodyText.Trim(),
                message.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                (message.Attachments ?? []).Select(attachment => new AttachmentRow(
                    attachment.FileName,
                    $"{attachment.ContentType} · {FormatAttachmentSize(attachment.SizeBytes)}")).ToList()))
            .ToList();

        MessagesCollection.ItemsSource = rows;
        EmptyLabel.IsVisible = rows.Count == 0;
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
    }

    private void SetBusy(bool busy)
    {
        SetLoading(busy);
        ReplyButton.IsEnabled = !busy;
        ReplyEditor.IsEnabled = !busy;
        ReplyAttachmentsCollection.IsEnabled = !busy;
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

    private void ShowReplyError(string message)
    {
        ReplyErrorLabel.Text = message;
        ReplyErrorBorder.IsVisible = true;
    }

    private void HideReplyError()
    {
        ReplyErrorLabel.Text = string.Empty;
        ReplyErrorBorder.IsVisible = false;
    }

    private static async Task NavigateBackAsync()
    {
        var shell = Shell.Current;
        if (shell is null)
            return;

        await shell.GoToAsync("..");
    }

    private static string GetStatusLabel(string status)
        => status switch
        {
            Models.SupportStatus.AguardandoAtendimento => "Aguardando atendimento",
            Models.SupportStatus.EmAtendimento => "Em atendimento",
            Models.SupportStatus.AguardandoRespostaUsuario => "Aguardando resposta do usuario",
            Models.SupportStatus.Encerrado => "Encerrado",
            Models.SupportStatus.Cancelado => "Cancelado",
            _ => status
        };

    private static string GetSenderTypeLabel(string senderType)
        => string.Equals(senderType, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Usuario";

    private static string FormatAttachmentSize(long sizeBytes)
    {
        if (sizeBytes >= 1024 * 1024)
            return $"{(sizeBytes / (1024d * 1024d)):0.00} MB";

        return $"{Math.Max(1, (int)Math.Round(sizeBytes / 1024d))} KB";
    }

    private static (Color, Color, Color) Palette(string background, string stroke, string text)
        => (Color.FromArgb(background), Color.FromArgb(stroke), Color.FromArgb(text));

    private sealed record MessageRow(
        string SenderName,
        string SenderTypeLabel,
        string BodyText,
        string CreatedAtLabel,
        List<AttachmentRow> Attachments)
    {
        public bool HasAttachments => Attachments.Count > 0;
    }

    private sealed record AttachmentRow(string FileName, string MetaLabel);

    private void RefreshReplyAttachments()
    {
        ReplyAttachmentsCollection.ItemsSource = null;
        ReplyAttachmentsCollection.ItemsSource = _replyAttachments.ToList();
        ReplyAttachmentsCollection.IsVisible = _replyAttachments.Count > 0;
    }

    private sealed record ReplyAttachmentRow(FileResult File, string FileName, string SizeLabel);
}
