using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.RequestObjects;

namespace server.Controllers;

[ApiController]
[AllowAnonymous]
public class LandingVisitsController : ControllerBase
{
    private readonly Context _context;

    public LandingVisitsController(IDbContextFactory<Context> contextFactory)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpPost("public/landing-visits")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Register([FromBody] LandingVisitRequest? request)
    {
        var landingVisit = new LandingVisit
        {
            visit = NormalizeVisit(request?.visit),
            ip_address = Limit(GetClientIpAddress(), 64) ?? "unknown",
            user_agent = Limit(Request.Headers.UserAgent.ToString(), 512),
            referrer = Limit(Request.Headers.Referer.ToString(), 1024),
            created_at = DateTime.UtcNow
        };

        _context.landing_visits.Add(landingVisit);
        _context.SaveChanges();

        return NoContent();
    }

    private string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstIp = forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstIp))
                return firstIp;
        }

        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string NormalizeVisit(string? visit)
    {
        var normalized = (visit ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return "direct";

        return Limit(normalized, 120) ?? "direct";
    }

    private static string? Limit(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }
}
