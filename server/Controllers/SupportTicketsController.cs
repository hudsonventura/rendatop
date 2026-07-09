using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

namespace server.Controllers;

[ApiController]
public class SupportTicketsController : AuthenticatedController
{
    private readonly Context _context;

    public SupportTicketsController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("support/tickets")]
    [ProducesResponseType(typeof(SupportTicketListResponse), StatusCodes.Status200OK)]
    public ActionResult<SupportTicketListResponse> List(
        [FromQuery] string scope = "open",
        [FromQuery] SupportTicketStatus? status = null,
        [FromQuery] string? search = null)
    {
        var visibleTicketsQuery = BuildVisibleTicketsQuery();

        var counts = new SupportTicketListCountsResponse(
            visibleTicketsQuery.Count(ticket => ticket.archived_at == null),
            visibleTicketsQuery.Count(ticket => ticket.archived_at != null),
            visibleTicketsQuery.Count(ticket =>
                ticket.archived_at == null &&
                (ticket.status == SupportTicketStatus.AguardandoAtendimento ||
                 ticket.status == SupportTicketStatus.EmAtendimento)),
            visibleTicketsQuery.Count(ticket =>
                ticket.archived_at == null &&
                ticket.status == SupportTicketStatus.AguardandoRespostaUsuario));

        var query = visibleTicketsQuery;

        if (scope.Equals("archived", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(ticket => ticket.archived_at != null);
        }
        else if (scope.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(ticket => ticket.archived_at == null);
        }

        if (status.HasValue)
            query = query.Where(ticket => ticket.status == status.Value);

        var normalizedSearch = (search ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(ticket =>
                ticket.subject.ToLower().Contains(normalizedSearch) ||
                ticket.requester_user_name.ToLower().Contains(normalizedSearch) ||
                ticket.requester_user_email.ToLower().Contains(normalizedSearch) ||
                _context.support_ticket_messages.Any(message =>
                    message.ticket_id == ticket.id &&
                    message.body_text.ToLower().Contains(normalizedSearch)));
        }

        var tickets = query
            .AsNoTracking()
            .OrderBy(ticket => ticket.archived_at != null ? 1 : 0)
            .ThenByDescending(ticket => ticket.last_message_at)
            .ToList();

        var ticketIds = tickets.Select(ticket => ticket.id).ToList();
        var relatedMessages = _context.support_ticket_messages
            .AsNoTracking()
            .Where(message => ticketIds.Contains(message.ticket_id))
            .ToList();

        var latestMessageByTicket = relatedMessages
            .GroupBy(message => message.ticket_id)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(message => message.created_at).First());

        var messageCountByTicket = relatedMessages
            .GroupBy(message => message.ticket_id)
            .ToDictionary(group => group.Key, group => group.Count());

        var items = tickets.Select(ticket =>
        {
            latestMessageByTicket.TryGetValue(ticket.id, out var latestMessage);
            var messageCount = messageCountByTicket.GetValueOrDefault(ticket.id, 0);

            return new SupportTicketListItemResponse(
                ticket.id,
                ticket.subject,
                ticket.status,
                ticket.archived_at != null,
                GetPendingFor(ticket.status),
                ticket.requester_user_name,
                ticket.requester_user_email,
                latestMessage?.sender_user_name,
                TruncatePreview(latestMessage?.body_text),
                messageCount,
                ticket.last_message_at,
                ticket.created_at);
        }).ToList();

        return Ok(new SupportTicketListResponse(items, counts));
    }

    [HttpGet("support/tickets/{id}")]
    [ProducesResponseType(typeof(SupportTicketDetailResponse), StatusCodes.Status200OK)]
    public ActionResult<SupportTicketDetailResponse> Get([FromRoute] Guid id)
    {
        var ticket = GetVisibleTicketOrThrow(id);
        return Ok(BuildDetailResponse(ticket));
    }

    private SupportTicketDetailResponse BuildDetailResponse(SupportTicket ticket)
    {
        var messages = _context.support_ticket_messages
            .AsNoTracking()
            .Where(message => message.ticket_id == ticket.id)
            .OrderBy(message => message.created_at)
            .ToList();

        var attachments = _context.support_ticket_message_attachments
            .AsNoTracking()
            .Where(attachment => messages.Select(message => message.id).Contains(attachment.message_id))
            .OrderBy(attachment => attachment.created_at)
            .ToList();

        var attachmentsByMessageId = attachments
            .GroupBy(attachment => attachment.message_id)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(attachment => new SupportTicketAttachmentResponse(
                        attachment.id,
                        attachment.file_name,
                        attachment.content_type,
                        attachment.size_bytes,
                        attachment.is_image))
                    .ToList());

        var history = _context.support_ticket_status_history
            .AsNoTracking()
            .Where(item => item.ticket_id == ticket.id)
            .OrderBy(item => item.created_at)
            .Select(item => new SupportTicketStatusHistoryResponse(
                item.id,
                item.actor_user_id,
                item.actor_user_name,
                item.from_status,
                item.to_status,
                item.source,
                item.created_at))
            .ToList();

        return new SupportTicketDetailResponse(
            ticket.id,
            ticket.subject,
            ticket.status,
            ticket.archived_at != null,
            GetPendingFor(ticket.status),
            ticket.requester_user_id,
            ticket.requester_user_name,
            ticket.requester_user_email,
            CanCurrentUserReply(ticket),
            IsAdmin && ticket.archived_at == null,
            messages.Select(message => new SupportTicketMessageResponse(
                message.id,
                message.sender_user_id,
                message.sender_user_type,
                message.sender_user_name,
                message.body_html,
                message.body_text,
                attachmentsByMessageId.GetValueOrDefault(message.id, []),
                message.created_at)).ToList(),
            history,
            ticket.last_message_at,
            ticket.created_at,
            ticket.updated_at,
            ticket.archived_at);
    }

    [HttpPost("support/tickets")]
    [ProducesResponseType(typeof(SupportTicketDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupportTicketDetailResponse>> Create(
        [FromForm] CreateSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (IsAdmin)
            throw new ExpectedException("Abertura de chamado está disponível apenas para usuários clientes.", HttpStatusCode.Forbidden);

        var subject = (request.subject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subject))
            throw new ExpectedException("Assunto é obrigatório.", HttpStatusCode.BadRequest);

        if (subject.Length > 180)
            throw new ExpectedException("Assunto pode ter no máximo 180 caracteres.", HttpStatusCode.BadRequest);

        var validatedAttachments = await ValidateAttachmentsAsync(request.attachments, cancellationToken);
        var (bodyHtml, bodyText) = SanitizeMessageBody(request.body_html);
        EnsureMessageHasContent(bodyText, validatedAttachments.Count);

        var now = DateTime.UtcNow;
        var ticket = new SupportTicket
        {
            requester_user_id = _user.id,
            requester_user_name = _user.name,
            requester_user_email = _user.email,
            subject = subject,
            status = SupportTicketStatus.AguardandoAtendimento,
            created_at = now,
            updated_at = now,
            last_message_at = now
        };

        var message = new SupportTicketMessage
        {
            ticket = ticket,
            sender_user_id = _user.id,
            sender_user_type = _user.user_type,
            sender_user_name = _user.name,
            body_html = bodyHtml,
            body_text = bodyText,
            created_at = now
        };

        var history = new SupportTicketStatusHistory
        {
            ticket = ticket,
            actor_user_id = _user.id,
            actor_user_name = _user.name,
            from_status = null,
            to_status = SupportTicketStatus.AguardandoAtendimento,
            source = SupportTicketChangeSource.SystemOnCreate,
            created_at = now
        };

        _context.support_tickets.Add(ticket);
        _context.support_ticket_messages.Add(message);
        _context.support_ticket_status_history.Add(history);

        foreach (var attachment in validatedAttachments)
        {
            _context.support_ticket_message_attachments.Add(new SupportTicketMessageAttachment
            {
                message = message,
                file_name = attachment.FileName,
                content_type = attachment.ContentType,
                size_bytes = attachment.SizeBytes,
                is_image = attachment.IsImage,
                content = attachment.Content,
                created_at = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = ticket.id }, BuildDetailResponse(ticket));
    }

    [HttpPost("support/tickets/{id}/messages")]
    [ProducesResponseType(typeof(SupportTicketDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupportTicketDetailResponse>> AddMessage(
        [FromRoute] Guid id,
        [FromForm] AddSupportTicketMessageRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = GetVisibleTicketTrackedOrThrow(id);

        if (ticket.archived_at != null)
            throw new ExpectedException("Chamados arquivados não aceitam novas mensagens.", HttpStatusCode.BadRequest);

        if (!IsAdmin)
        {
            if (ticket.status != SupportTicketStatus.AguardandoRespostaUsuario)
                throw new ExpectedException("Você só pode responder quando o chamado estiver aguardando sua resposta.", HttpStatusCode.BadRequest);
        }

        var validatedAttachments = await ValidateAttachmentsAsync(request.attachments, cancellationToken);
        var (bodyHtml, bodyText) = SanitizeMessageBody(request.body_html);
        EnsureMessageHasContent(bodyText, validatedAttachments.Count);

        var now = DateTime.UtcNow;
        var message = new SupportTicketMessage
        {
            ticket_id = ticket.id,
            sender_user_id = _user.id,
            sender_user_type = _user.user_type,
            sender_user_name = _user.name,
            body_html = bodyHtml,
            body_text = bodyText,
            created_at = now
        };

        _context.support_ticket_messages.Add(message);

        foreach (var attachment in validatedAttachments)
        {
            _context.support_ticket_message_attachments.Add(new SupportTicketMessageAttachment
            {
                message = message,
                file_name = attachment.FileName,
                content_type = attachment.ContentType,
                size_bytes = attachment.SizeBytes,
                is_image = attachment.IsImage,
                content = attachment.Content,
                created_at = now
            });
        }

        ApplyPostReplyStatus(ticket, now);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(BuildDetailResponse(ticket));
    }

    [HttpPost("support/tickets/{id}/status")]
    [ProducesResponseType(typeof(SupportTicketDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupportTicketDetailResponse>> ChangeStatus(
        [FromRoute] Guid id,
        [FromBody] ChangeSupportTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var ticket = GetVisibleTicketTrackedOrThrow(id);
        if (ticket.archived_at != null)
            throw new ExpectedException("Chamados arquivados não podem ter o status alterado.", HttpStatusCode.BadRequest);

        if (ticket.status == request.status)
            throw new ExpectedException("O chamado já está neste status.", HttpStatusCode.BadRequest);

        var now = DateTime.UtcNow;
        var previousStatus = ticket.status;
        ticket.status = request.status;
        ticket.updated_at = now;

        if (request.status is SupportTicketStatus.Encerrado or SupportTicketStatus.Cancelado)
            ticket.archived_at = now;

        _context.support_ticket_status_history.Add(new SupportTicketStatusHistory
        {
            ticket_id = ticket.id,
            actor_user_id = _user.id,
            actor_user_name = _user.name,
            from_status = previousStatus,
            to_status = request.status,
            source = SupportTicketChangeSource.AdminManual,
            created_at = now
        });

        if (request.status == SupportTicketStatus.AguardandoRespostaUsuario)
        {
            _context.notifications.Add(new Notification
            {
                id = SnowflakeGuid.NewGuid(),
                user_id = ticket.requester_user_id,
                title = "Chamado atualizado",
                message = $"Seu chamado \"{ticket.subject}\" está aguardando sua resposta.",
                is_read = false,
                created_at = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(BuildDetailResponse(ticket));
    }

    [HttpGet("support/attachments/{attachmentId}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public IActionResult DownloadAttachment([FromRoute] Guid attachmentId)
    {
        var attachment = _context.support_ticket_message_attachments
            .AsNoTracking()
            .Where(item => item.id == attachmentId)
            .Select(item => new
            {
                item.id,
                item.file_name,
                item.content_type,
                item.content,
                item.message.ticket_id,
                item.message.ticket.requester_user_id
            })
            .FirstOrDefault();

        if (attachment is null)
            throw new ExpectedException("Anexo não encontrado.", HttpStatusCode.NotFound);

        if (!IsAdmin && attachment.requester_user_id != _user.id)
            throw new ExpectedException("Você não tem acesso a este anexo.", HttpStatusCode.Forbidden);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(attachment.content, attachment.content_type, attachment.file_name);
    }

    private IQueryable<SupportTicket> BuildVisibleTicketsQuery()
    {
        var query = _context.support_tickets.AsQueryable();
        if (!IsAdmin)
            query = query.Where(ticket => ticket.requester_user_id == _user.id);

        return query;
    }

    private SupportTicket GetVisibleTicketOrThrow(Guid id)
    {
        var ticket = BuildVisibleTicketsQuery()
            .AsNoTracking()
            .FirstOrDefault(item => item.id == id);

        return ticket ?? throw new ExpectedException("Chamado não encontrado.", HttpStatusCode.NotFound);
    }

    private SupportTicket GetVisibleTicketTrackedOrThrow(Guid id)
    {
        var ticket = BuildVisibleTicketsQuery()
            .FirstOrDefault(item => item.id == id);

        return ticket ?? throw new ExpectedException("Chamado não encontrado.", HttpStatusCode.NotFound);
    }

    private async Task<List<SupportValidatedAttachment>> ValidateAttachmentsAsync(
        IEnumerable<IFormFile>? attachments,
        CancellationToken cancellationToken)
    {
        var result = new List<SupportValidatedAttachment>();
        if (attachments is null)
            return result;

        foreach (var attachment in attachments)
            result.Add(await SupportAttachmentRules.ValidateAsync(attachment, cancellationToken));

        return result;
    }

    private static (string BodyHtml, string BodyText) SanitizeMessageBody(string? rawHtml)
    {
        var bodyHtml = SupportRichTextSanitizer.Sanitize(rawHtml);
        var bodyText = SupportRichTextSanitizer.ToPlainText(bodyHtml);
        return (bodyHtml, bodyText);
    }

    private static void EnsureMessageHasContent(string bodyText, int attachmentCount)
    {
        if (string.IsNullOrWhiteSpace(bodyText) && attachmentCount == 0)
            throw new ExpectedException("A mensagem precisa ter texto ou anexo.", HttpStatusCode.BadRequest);
    }

    private bool CanCurrentUserReply(SupportTicket ticket)
    {
        if (ticket.archived_at != null)
            return false;

        if (IsAdmin)
            return true;

        return ticket.requester_user_id == _user.id &&
               ticket.status == SupportTicketStatus.AguardandoRespostaUsuario;
    }

    private void ApplyPostReplyStatus(SupportTicket ticket, DateTime now)
    {
        ticket.updated_at = now;
        ticket.last_message_at = now;

        if (IsAdmin || ticket.status == SupportTicketStatus.AguardandoAtendimento)
            return;

        var previousStatus = ticket.status;
        ticket.status = SupportTicketStatus.AguardandoAtendimento;

        _context.support_ticket_status_history.Add(new SupportTicketStatusHistory
        {
            ticket_id = ticket.id,
            actor_user_id = _user.id,
            actor_user_name = _user.name,
            from_status = previousStatus,
            to_status = ticket.status,
            source = SupportTicketChangeSource.SystemOnUserReply,
            created_at = now
        });
    }

    private static string? GetPendingFor(SupportTicketStatus status) => status switch
    {
        SupportTicketStatus.AguardandoAtendimento => "admin",
        SupportTicketStatus.EmAtendimento => "admin",
        SupportTicketStatus.AguardandoRespostaUsuario => "user",
        _ => null
    };

    private static string? TruncatePreview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = text.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Trim();

        return normalized.Length <= 140
            ? normalized
            : $"{normalized[..140].TrimEnd()}...";
    }
}

public class CreateSupportTicketRequest
{
    public string? subject { get; set; }
    public string? body_html { get; set; }
    public List<IFormFile>? attachments { get; set; }
}

public class AddSupportTicketMessageRequest
{
    public string? body_html { get; set; }
    public List<IFormFile>? attachments { get; set; }
}

public record ChangeSupportTicketStatusRequest(SupportTicketStatus status);

public record SupportTicketListResponse(
    IReadOnlyList<SupportTicketListItemResponse> items,
    SupportTicketListCountsResponse counts
);

public record SupportTicketListCountsResponse(
    int open_count,
    int archived_count,
    int waiting_admin_count,
    int waiting_user_count
);

public record SupportTicketListItemResponse(
    Guid id,
    string subject,
    SupportTicketStatus status,
    bool is_archived,
    string? pending_for,
    string requester_user_name,
    string requester_user_email,
    string? latest_sender_user_name,
    string? latest_message_preview,
    int message_count,
    DateTime last_message_at,
    DateTime created_at
);

public record SupportTicketDetailResponse(
    Guid id,
    string subject,
    SupportTicketStatus status,
    bool is_archived,
    string? pending_for,
    Guid requester_user_id,
    string requester_user_name,
    string requester_user_email,
    bool can_current_user_reply,
    bool can_current_user_change_status,
    IReadOnlyList<SupportTicketMessageResponse> messages,
    IReadOnlyList<SupportTicketStatusHistoryResponse> status_history,
    DateTime last_message_at,
    DateTime created_at,
    DateTime updated_at,
    DateTime? archived_at
);

public record SupportTicketMessageResponse(
    Guid id,
    Guid sender_user_id,
    UserType sender_user_type,
    string sender_user_name,
    string body_html,
    string body_text,
    IReadOnlyList<SupportTicketAttachmentResponse> attachments,
    DateTime created_at
);

public record SupportTicketAttachmentResponse(
    Guid id,
    string file_name,
    string content_type,
    long size_bytes,
    bool is_image
);

public record SupportTicketStatusHistoryResponse(
    Guid id,
    Guid actor_user_id,
    string actor_user_name,
    SupportTicketStatus? from_status,
    SupportTicketStatus to_status,
    SupportTicketChangeSource source,
    DateTime created_at
);
