using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;

namespace server.Controllers;

[ApiController]
public class NotificationsController : AuthenticatedController
{
    private readonly Context _context;

    public NotificationsController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("Notifications")]
    [ProducesResponseType(typeof(List<NotificationListItemResponse>), StatusCodes.Status200OK)]
    public IActionResult List([FromQuery] int limit = 30, [FromQuery] bool unread_only = false)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);

        var query = _context.notifications
            .AsNoTracking()
            .Where(n => n.user_id == _user.id);

        if (unread_only)
            query = query.Where(n => !n.is_read);

        var notifications = query
            .OrderByDescending(n => n.created_at)
            .Take(safeLimit)
            .Select(n => new NotificationListItemResponse(
                n.id,
                n.title,
                n.message,
                n.is_read,
                n.created_at,
                n.read_at))
            .ToList();

        return Ok(notifications);
    }

    [HttpGet("Notifications/UnreadCount")]
    [ProducesResponseType(typeof(NotificationUnreadCountResponse), StatusCodes.Status200OK)]
    public IActionResult UnreadCount()
    {
        var count = _context.notifications.Count(n => n.user_id == _user.id && !n.is_read);
        return Ok(new NotificationUnreadCountResponse(count));
    }

    [HttpPost("Notifications/{id}/Read")]
    [ProducesResponseType(typeof(NotificationUnreadCountResponse), StatusCodes.Status200OK)]
    public IActionResult MarkAsRead([FromRoute] Guid id)
    {
        var notification = _context.notifications.FirstOrDefault(n => n.id == id && n.user_id == _user.id);
        if (notification is null)
            return NotFound();

        if (!notification.is_read)
        {
            notification.is_read = true;
            notification.read_at = DateTime.UtcNow;
            _context.SaveChanges();
        }

        var unread = _context.notifications.Count(n => n.user_id == _user.id && !n.is_read);
        return Ok(new NotificationUnreadCountResponse(unread));
    }

    [HttpPost("Notifications/ReadAll")]
    [ProducesResponseType(typeof(NotificationUnreadCountResponse), StatusCodes.Status200OK)]
    public IActionResult MarkAllAsRead()
    {
        var unread = _context.notifications
            .Where(n => n.user_id == _user.id && !n.is_read)
            .ToList();

        if (unread.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var item in unread)
            {
                item.is_read = true;
                item.read_at = now;
            }
            _context.SaveChanges();
        }

        return Ok(new NotificationUnreadCountResponse(0));
    }
}

public record NotificationListItemResponse(
    Guid id,
    string title,
    string message,
    bool is_read,
    DateTime created_at,
    DateTime? read_at);

public record NotificationUnreadCountResponse(int unread_count);
