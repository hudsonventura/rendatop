using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
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
        var email = credentials.email?.Trim().ToLowerInvariant() ?? string.Empty;

        var user = _context.users.AsNoTracking().Where(x => x.email == email).FirstOrDefault();
        if (user is null)
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta", System.Net.HttpStatusCode.Unauthorized);

        if (!user.CheckPass(credentials.password))
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta", System.Net.HttpStatusCode.Unauthorized);

        return CreateSession(user);
    }

    /// <summary>
    /// Cria uma nova conta e define o cookie JWT HttpOnly
    /// </summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult Signup([FromBody] SignUpRequest request)
    {
        var name = request.name?.Trim() ?? string.Empty;
        var email = request.email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            throw new ExpectedException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ExpectedException("A senha deve ter pelo menos 6 caracteres.");

        bool alreadyExists = _context.users.AsNoTracking().Any(x => x.email == email);
        if (alreadyExists)
            throw new ExpectedException("Já existe uma conta com esse email.", HttpStatusCode.Conflict);

        User user = new User
        {
            id = SnowflakeGuid.NewGuid(),
            name = name,
            email = email,
            password = password
        };

        _context.users.Add(user);
        _context.SaveChanges();

        return CreateSession(user);
    }

    private IActionResult CreateSession(User user)
    {
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
public record SignUpRequest(string name, string email, string password);
