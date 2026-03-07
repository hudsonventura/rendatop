using server.Domain;
using server.Utils;
using StackExchange.Redis;

namespace server.Middlewares;

public static class AuthenticationMiddlewarePlugin
{
    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}


public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private StackExchange.Redis.IDatabase _redis;

    public AuthenticationMiddleware(RequestDelegate next, IConnectionMultiplexer muxer_redis)
    {
        _redis = muxer_redis.GetDatabase();
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Skip authentication for public paths
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Read the JWT from the HttpOnly cookie (falls back to Authorization header for backwards compat / Scalar UI)
        string? token = context.Request.Cookies["jwt"];

        if (string.IsNullOrEmpty(token))
        {
            // Fallback: accept Bearer token from Authorization header (e.g. Scalar / Swagger UI)
            var authorization = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                token = authorization["Bearer ".Length..];
        }

        if (!string.IsNullOrEmpty(token))
        {
            string? json = _redis.StringGet(token);
            if (json is null)
            {
                // Token not found in cache — expired or invalid
                throw new ExpectedException("Token de autenticação ausente ou inválido...", System.Net.HttpStatusCode.Unauthorized);
            }

            // Inject the User into HttpContext.Items so controllers can access it
            User user = User.Deserialize(json);
            context.Items["User"] = user;

            // Slide the expiration so active users don't get kicked out
            TimeSpan timeSpanUntilExpiration = TimeSpan.FromDays(30);
            _redis.StringSetAsync(token, user.GetJsonSerialized(), timeSpanUntilExpiration);
        }

        await _next(context);
    }
}
