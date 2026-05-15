using System.Net;
using System.Text;
using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class SupportTicketService
{
    private const string TicketsCacheKey = "support-tickets-all";
    private const string DetailCachePrefix = "support-ticket-detail-";
    private const long AttachmentMaxBytes = 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".gif",
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx"
    };

    private readonly ApiClient _apiClient;
    private readonly LocalSnapshotStore _snapshots;

    public SupportTicketService(ApiClient apiClient, LocalSnapshotStore snapshots)
    {
        _apiClient = apiClient;
        _snapshots = snapshots;
    }

    public async Task<SupportTicketListResponseDto> GetTicketsAsync(
        string scope,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"scope={Uri.EscapeDataString(string.IsNullOrWhiteSpace(scope) ? SupportScope.Open : scope)}"
        };

        if (!string.IsNullOrWhiteSpace(status))
            query.Add($"status={Uri.EscapeDataString(status)}");

        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");

        var path = "/support/tickets";
        if (query.Count > 0)
            path = $"{path}?{string.Join("&", query)}";

        return await _apiClient.GetAsync<SupportTicketListResponseDto>(path, cancellationToken)
            ?? new SupportTicketListResponseDto
            {
                Items = [],
                Counts = new SupportTicketListCountsDto()
            };
    }

    public async Task<SupportTicketListResponseDto> RefreshAllTicketsCacheAsync(CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetAsync<SupportTicketListResponseDto>("/support/tickets?scope=all", cancellationToken)
            ?? new SupportTicketListResponseDto
            {
                Items = [],
                Counts = new SupportTicketListCountsDto()
            };

        await _snapshots.SetAsync(TicketsCacheKey, response, cancellationToken);
        return response;
    }

    public async Task<SupportTicketListResponseDto> GetCachedTicketsAsync(CancellationToken cancellationToken = default)
        => await _snapshots.GetAsync<SupportTicketListResponseDto>(TicketsCacheKey, cancellationToken)
           ?? new SupportTicketListResponseDto
           {
               Items = [],
               Counts = new SupportTicketListCountsDto()
           };

    public async Task<SupportTicketDetailDto> CreateTicketAsync(
        string subject,
        string bodyText,
        IReadOnlyList<FileResult>? attachments,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(subject.Trim(), Encoding.UTF8), "subject");
        content.Add(new StringContent(ToSimpleHtml(bodyText), Encoding.UTF8), "body_html");

        foreach (var attachment in attachments ?? [])
        {
            await ValidateAttachmentAsync(attachment);

            var stream = await attachment.OpenReadAsync();
            var streamContent = new StreamContent(stream);
            if (!string.IsNullOrWhiteSpace(attachment.ContentType))
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(attachment.ContentType);

            content.Add(streamContent, "attachments", attachment.FileName);
        }

        var detail = await _apiClient.PostMultipartAsync<SupportTicketDetailDto>("/support/tickets", content, cancellationToken)
            ?? throw new ApiException("Resposta de atendimento invalida.", 500);

        await _snapshots.SetAsync($"{DetailCachePrefix}{detail.Id}", detail, cancellationToken);
        return detail;
    }

    public async Task<SupportTicketDetailDto> GetTicketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var detail = await _apiClient.GetAsync<SupportTicketDetailDto>($"/support/tickets/{id}", cancellationToken)
           ?? throw new ApiException("Resposta de atendimento invalida.", 500);

        await _snapshots.SetAsync($"{DetailCachePrefix}{id}", detail, cancellationToken);
        return detail;
    }

    public async Task<SupportTicketDetailDto?> GetCachedTicketAsync(Guid id, CancellationToken cancellationToken = default)
        => await _snapshots.GetAsync<SupportTicketDetailDto>($"{DetailCachePrefix}{id}", cancellationToken);

    public async Task<SupportTicketDetailDto> AddMessageAsync(
        Guid id,
        string bodyText,
        IReadOnlyList<FileResult>? attachments,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(ToSimpleHtml(bodyText), Encoding.UTF8), "body_html");

        foreach (var attachment in attachments ?? [])
        {
            await ValidateAttachmentAsync(attachment);

            var stream = await attachment.OpenReadAsync();
            var streamContent = new StreamContent(stream);
            if (!string.IsNullOrWhiteSpace(attachment.ContentType))
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(attachment.ContentType);

            content.Add(streamContent, "attachments", attachment.FileName);
        }

        var detail = await _apiClient.PostMultipartAsync<SupportTicketDetailDto>($"/support/tickets/{id}/messages", content, cancellationToken)
            ?? throw new ApiException("Resposta de atendimento invalida.", 500);

        await _snapshots.SetAsync($"{DetailCachePrefix}{id}", detail, cancellationToken);
        return detail;
    }

    public bool IsAllowedAttachment(FileResult file)
    {
        var extension = Path.GetExtension(file.FileName ?? string.Empty);
        return AllowedExtensions.Contains(extension);
    }

    public async Task<long> GetAttachmentSizeAsync(FileResult file)
    {
        await using var stream = await file.OpenReadAsync();
        return stream.Length;
    }

    public string FormatAttachmentSize(long sizeBytes)
    {
        if (sizeBytes >= 1024 * 1024)
            return $"{(sizeBytes / (1024d * 1024d)):0.00} MB";

        return $"{Math.Max(1, (int)Math.Round(sizeBytes / 1024d))} KB";
    }

    private async Task ValidateAttachmentAsync(FileResult file)
    {
        if (!IsAllowedAttachment(file))
            throw new ApiException("Formato de anexo nao permitido. Use imagens, PDF ou arquivos Office.", 400);

        var size = await GetAttachmentSizeAsync(file);
        if (size > AttachmentMaxBytes)
            throw new ApiException("Cada anexo pode ter no maximo 1 MB.", 400);
    }

    private static string ToSimpleHtml(string value)
    {
        var lines = (value ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => WebUtility.HtmlEncode(line.TrimEnd()))
            .ToList();

        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        if (nonEmptyLines.Count == 0)
            return string.Empty;

        return $"<p>{string.Join("<br />", nonEmptyLines)}</p>";
    }
}
