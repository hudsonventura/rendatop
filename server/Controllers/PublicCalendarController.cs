using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;

namespace server.Controllers;

[ApiController]
[AllowAnonymous]
public class PublicCalendarController : ControllerBase
{
    private readonly Context _context;

    public PublicCalendarController(IDbContextFactory<Context> contextFactory)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("public/calendar/{token:guid}.ics")]
    [Produces("text/calendar")]
    public IActionResult GetCalendar(Guid token)
    {
        var user = _context.users
            .AsNoTracking()
            .FirstOrDefault(u =>
                u.calendar_public_enabled &&
                u.calendar_public_token == token);

        if (user is null || !CanUseCalendarIcs(user.id))
            return NotFound();

        var investments = _context.investments
            .AsNoTracking()
            .Include(i => i.bank)
            .Where(i => i.owner.id == user.id && i.due_date != null)
            .OrderBy(i => i.due_date)
            .ToList();

        var nowUtc = DateTime.UtcNow;
        var sb = new StringBuilder();

        AppendLine(sb, "BEGIN:VCALENDAR");
        AppendLine(sb, "VERSION:2.0");
        AppendLine(sb, "PRODID:-//RentaTop//Investments Calendar//PT-BR");
        AppendLine(sb, "CALSCALE:GREGORIAN");
        AppendLine(sb, "METHOD:PUBLISH");
        AppendLine(sb, $"X-WR-CALNAME:{EscapeIcsText($"RentaTop - Vencimentos de {user.name}")}");
        AppendLine(sb, "X-WR-TIMEZONE:UTC");

        foreach (var investment in investments)
        {
            var dueDate = ((DateTime)investment.due_date!).ToUniversalTime().Date;
            var uid = $"{investment.id}@rendatop";
            var bankName = investment.bank?.Name ?? "Banco não informado";
            var indexLabel = GetIndexLabel(investment);
            var liquidValue = GetLiquidValue(investment);
            var description =
                $"📈 RentaTop" + Environment.NewLine +
                $"Banco: {bankName}" + Environment.NewLine +
                $"Título: {investment.title}" + Environment.NewLine +
                $"Valor investido: R$ {investment.value:N2}" + Environment.NewLine +
                $"Valor liquido: R$ {liquidValue:N2}" + Environment.NewLine +
                $"Indexador: {indexLabel}" + Environment.NewLine +
                $"Vencimento: {dueDate:dd/MM/yyyy}";

            AppendLine(sb, "BEGIN:VEVENT");
            AppendLine(sb, $"UID:{uid}");
            AppendLine(sb, $"DTSTAMP:{nowUtc:yyyyMMdd'T'HHmmss'Z'}");
            AppendLine(sb, $"DTSTART;VALUE=DATE:{dueDate:yyyyMMdd}");
            AppendLine(sb, $"SUMMARY:{EscapeIcsText($"Vencimento - {investment.title}")}");
            AppendLine(sb, $"DESCRIPTION:{EscapeIcsText(description)}");
            AppendLine(sb, "END:VEVENT");
        }

        AppendLine(sb, "END:VCALENDAR");

        Response.Headers["Content-Disposition"] = "inline; filename=rentatop-investments.ics";
        return Content(sb.ToString(), "text/calendar; charset=utf-8");
    }

    private static string GetIndexLabel(Investment investment)
    {
        return investment.index switch
        {
            IdexesType.PERCENT_YEAR => $"{investment.index_percent:N2}% a.a.",
            IdexesType.CDI => $"{investment.index_percent:N2}% CDI",
            IdexesType.CDI_MAIS => $"CDI + {investment.index_percent:N2}% a.a.",
            IdexesType.IPCA_MAIS => $"IPCA + {investment.index_value:N2}%",
            _ => $"{investment.index_percent:N2}%"
        };
    }

    private decimal GetLiquidValue(Investment investment)
    {
        var calcType = typeof(ICalculator).Assembly.GetType(
            $"server.Domain.Calculator_{investment.index}"
        );

        if (calcType is null)
            return investment.value;

        var calc = (ICalculator)Activator.CreateInstance(calcType, _context)!;
        var calculated = calc.Calculate(investment.ToRequest());
        return calculated.FirstOrDefault()?.value_liq ?? investment.value;
    }

    private static string EscapeIcsText(string? text)
    {
        return (text ?? string.Empty)
            .Replace(@"\", @"\\")
            .Replace(";", @"\;")
            .Replace(",", @"\,")
            .Replace("\r\n", @"\n")
            .Replace("\n", @"\n");
    }

    private bool CanUseCalendarIcs(Guid userId)
    {
        var planId = _context.subscriptions
            .AsNoTracking()
            .Where(s => s.user_id == userId && s.status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.created_at)
            .Select(s => s.plan_id)
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(planId) &&
            Plans.GetById(planId)?.calendar_ics == true;
    }

    private static void AppendLine(StringBuilder builder, string line)
    {
        builder.Append(line).Append("\r\n");
    }
}
