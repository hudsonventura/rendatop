using System.Diagnostics;

namespace server.Utils;

public static class TraceContext
{
    public static string GetTraceId()
    {
        if (Activity.Current is { } activity)
        {
            var traceId = activity.TraceId.ToString();
            if (!string.IsNullOrWhiteSpace(traceId))
                return traceId;

            if (!string.IsNullOrWhiteSpace(activity.Id))
                return activity.Id;
        }

        return Guid.NewGuid().ToString("N");
    }

    public static Activity StartActivity(string name)
    {
        var activity = new Activity(name);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        return activity;
    }
}
