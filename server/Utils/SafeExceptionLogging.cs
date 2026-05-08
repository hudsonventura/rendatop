using System.Text;
using Microsoft.EntityFrameworkCore;

namespace server.Utils;

public static class SafeExceptionLogging
{
    public static string ToSafeLogString(this Exception exception)
    {
        var builder = new StringBuilder();
        AppendException(builder, exception, 0);
        return builder.ToString();
    }

    private static void AppendException(StringBuilder builder, Exception exception, int depth)
    {
        if (depth > 0)
            builder.Append(" -> ");

        builder.Append(exception.GetType().Name);
        builder.Append(": ");
        builder.Append(exception.Message.Replace('\r', ' ').Replace('\n', ' ').Trim());

        if (exception is DbUpdateException dbUpdateException)
        {
            var entries = dbUpdateException.Entries
                .Select(entry => $"{entry.Metadata.ClrType.Name}[{entry.State}]")
                .Distinct()
                .ToArray();

            if (entries.Length > 0)
                builder.Append($" Entries=[{string.Join(", ", entries)}]");
        }

        if (exception.InnerException is not null)
            AppendException(builder, exception.InnerException, depth + 1);
    }
}
