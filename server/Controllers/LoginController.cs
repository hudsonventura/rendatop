using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;
using StackExchange.Redis;

namespace server.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly Context _context;
    private readonly IDatabase _redis;
    private readonly IWebHostEnvironment _env;

    public LoginController(Context context, IConnectionMultiplexer muxer_redis, IWebHostEnvironment env)
    {
        _context = context;
        _redis = muxer_redis.GetDatabase();
        _env = env;
    }

    /// <summary>
    /// Realiza o processo de login e define o cookie JWT HttpOnly
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRecord credentials)
    {
        var user = _context.users.AsNoTracking().Where(x => x.email == credentials.email).FirstOrDefault();
        if (user is null)
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta", System.Net.HttpStatusCode.Unauthorized);

        if (!user.CheckPass(credentials.password))
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta", System.Net.HttpStatusCode.Unauthorized);

        ITokenService token_service = new TokenServiceJWT();
        var token = token_service.Generate(user, "Common User");

        TimeSpan timeSpanUntilExpiration = TimeSpan.FromDays(30);
        _redis.StringSetAsync(token, user.GetJsonSerialized(), timeSpanUntilExpiration);

        // Determine if the cookie should be Secure based on env var or environment
        var cookieSecureEnv = Environment.GetEnvironmentVariable("COOKIE_SECURE");
        bool secureCookie = cookieSecureEnv?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
            || (!_env.IsDevelopment() && cookieSecureEnv != "false");

        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = secureCookie,
            Expires = DateTimeOffset.UtcNow.Add(timeSpanUntilExpiration),
            Path = "/"
        });

        // Return user display info (not the token — it lives only in the HttpOnly cookie)
        return Ok(new LoginResponse(user.name, user.email));
    }


    /// <summary>
    /// Realiza o logout limpando o cookie JWT
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt", new CookieOptions { Path = "/" });
        return Ok();
    }
}


public record LoginResponse(string name, string email);
