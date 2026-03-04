using api.services;

namespace api.middlewares;

/// <summary>
/// Middleware que intercepta todas as requisições e valida o token JWT presente no cookie "session".
/// Rotas marcadas com [Authorize] já são protegidas automaticamente pelo ASP.NET, mas este
/// middleware permite lógica adicional (logging, claims customizados, etc.) antes de chegar ao controller.
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    // Rotas que NÃO precisam de autenticação
    private static readonly HashSet<string> _publicRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/login",
        "/register",
        "/openapi/v1.json",
        "/scalar/v1",
    };

    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Deixa passar rotas públicas sem validar token
        if (IsPublicRoute(path))
        {
            await _next(context);
            return;
        }

        // Tenta ler o token do cookie "session"
        var token = context.Request.Cookies["session"];

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Requisição sem token para {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Token não fornecido." });
            return;
        }

        // Valida assinatura, issuer, audience e expiração
        var principal = JwtService.ValidateToken(token);

        if (principal is null)
        {
            _logger.LogWarning("Token inválido ou expirado para {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Token inválido ou expirado." });
            return;
        }

        // Injeta o ClaimsPrincipal no contexto para que os controllers possam usar User.Identity
        context.User = principal;

        _logger.LogInformation("Token válido. Usuário: {Name} | Rota: {Path}",
            principal.Identity?.Name, path);

        await _next(context);
    }

    private static bool IsPublicRoute(string path)
    {
        foreach (var route in _publicRoutes)
        {
            if (path.StartsWith(route, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

/// <summary>
/// Extension method para registrar o middleware de forma limpa no Program.cs
/// </summary>
public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<JwtMiddleware>();
}
