using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using server.Domain;
using server.Utils;
using StackExchange.Redis;

namespace server.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    private readonly Context _context;
    private readonly IDatabase _redis;
    private readonly IWebHostEnvironment _env;

    public LoginController(IDbContextFactory<Context> contextFactory, IConnectionMultiplexer muxer_redis, IWebHostEnvironment env)
    {
        _context = contextFactory.CreateDbContext();
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

        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.email == email);
        if (user is null || !user.CheckPass(credentials.password))
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta", HttpStatusCode.Unauthorized);

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

    /// <summary>
    /// Inicia o fluxo de autenticação do Google (OAuth2)
    /// </summary>
    [HttpGet("auth/google/login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(googleClientId))
            throw new ExpectedException("GOOGLE_CLIENT_ID não configurado no servidor.", HttpStatusCode.InternalServerError);

        var redirectUri = GetGoogleRedirectUri();
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        SetOAuthStateCookie("oauth_google_state", state);

        var googleAuthUrl =
            "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(googleClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString("openid email profile")}" +
            $"&state={Uri.EscapeDataString(state)}" +
            "&access_type=online" +
            "&prompt=select_account";

        return Redirect(googleAuthUrl);
    }

    /// <summary>
    /// Callback do Google OAuth2
    /// </summary>
    [HttpGet("auth/google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(error))
                return Redirect(BuildFrontLoginRedirect("google_error", "Autenticação Google cancelada ou recusada."));

            if (string.IsNullOrWhiteSpace(code))
                return Redirect(BuildFrontLoginRedirect("google_error", "Código de autenticação Google ausente."));

            ValidateOAuthState("oauth_google_state", state);

            var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
            if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
                return Redirect(BuildFrontLoginRedirect("google_error", "Configuração do Google SSO incompleta no servidor."));

            var redirectUri = GetGoogleRedirectUri();
            var userInfo = await GetGoogleUserInfo(code, googleClientId, googleClientSecret, redirectUri);

            if (!userInfo.EmailVerified || string.IsNullOrWhiteSpace(userInfo.Email))
                return Redirect(BuildFrontLoginRedirect("google_error", "Conta Google sem email verificado."));

            var login = EnsureUserAndCreateSession(userInfo.Email, userInfo.Name);
            return Redirect(BuildFrontLoginRedirect("google_success", null, login));
        }
        catch (Exception ex)
        {
            return Redirect(BuildFrontLoginRedirect("google_error", ex.Message));
        }
    }

    /// <summary>
    /// Inicia o fluxo de autenticação da Microsoft (OAuth2 / OpenID Connect)
    /// </summary>
    [HttpGet("auth/microsoft/login")]
    [AllowAnonymous]
    public IActionResult MicrosoftLogin()
    {
        var clientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");
        var tenantId = Environment.GetEnvironmentVariable("MICROSOFT_TENANT_ID") ?? "common";
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ExpectedException("MICROSOFT_CLIENT_ID não configurado no servidor.", HttpStatusCode.InternalServerError);

        var redirectUri = GetMicrosoftRedirectUri();
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        SetOAuthStateCookie("oauth_microsoft_state", state);

        var microsoftAuthUrl =
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId)}/oauth2/v2.0/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&response_mode=query" +
            $"&scope={Uri.EscapeDataString("openid profile email User.Read")}" +
            $"&state={Uri.EscapeDataString(state)}" +
            "&prompt=select_account";

        return Redirect(microsoftAuthUrl);
    }

    /// <summary>
    /// Callback do Microsoft OAuth2
    /// </summary>
    [HttpGet("auth/microsoft/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> MicrosoftCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? error_description)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(error))
                return Redirect(BuildFrontLoginRedirect("microsoft_error", $"Autenticação Microsoft recusada: {error_description ?? error}"));

            if (string.IsNullOrWhiteSpace(code))
                return Redirect(BuildFrontLoginRedirect("microsoft_error", "Código de autenticação Microsoft ausente."));

            ValidateOAuthState("oauth_microsoft_state", state);

            var clientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET")
                ?? Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET_VALUE");
            var tenantId = Environment.GetEnvironmentVariable("MICROSOFT_TENANT_ID") ?? "common";

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                return Redirect(BuildFrontLoginRedirect("microsoft_error", "Configuração do Microsoft SSO incompleta no servidor."));

            var redirectUri = GetMicrosoftRedirectUri();
            var userInfo = await GetMicrosoftUserInfo(code, clientId, clientSecret, tenantId, redirectUri);

            if (string.IsNullOrWhiteSpace(userInfo.Email))
                return Redirect(BuildFrontLoginRedirect("microsoft_error", "Não foi possível identificar o email da conta Microsoft."));

            var login = EnsureUserAndCreateSession(userInfo.Email, userInfo.Name);
            return Redirect(BuildFrontLoginRedirect("microsoft_success", null, login));
        }
        catch (Exception ex)
        {
            return Redirect(BuildFrontLoginRedirect("microsoft_error", ex.Message));
        }
    }

    private IActionResult CreateSession(User user) => Ok(SetSession(user));

    private LoginResponse EnsureUserAndCreateSession(string emailInput, string? nameInput)
    {
        var email = emailInput.Trim().ToLowerInvariant();
        var name = string.IsNullOrWhiteSpace(nameInput) ? email : nameInput.Trim();

        var user = _context.users.FirstOrDefault(x => x.email == email);
        if (user is null)
        {
            user = new User
            {
                id = SnowflakeGuid.NewGuid(),
                name = name,
                email = email,
                password = Guid.NewGuid().ToString("N")
            };
            _context.users.Add(user);
            _context.SaveChanges();
        }

        return SetSession(user);
    }

    private LoginResponse SetSession(User user)
    {
        ITokenService token_service = new TokenServiceJWT();
        var token = token_service.Generate(user, "Common User");

        TimeSpan timeSpanUntilExpiration = TimeSpan.FromDays(30);
        _redis.StringSetAsync(token, user.GetJsonSerialized(), timeSpanUntilExpiration);

        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = IsSecureCookie(),
            Expires = DateTimeOffset.UtcNow.Add(timeSpanUntilExpiration),
            Path = "/"
        });

        return new LoginResponse(user.name, user.email);
    }

    private bool IsSecureCookie()
    {
        var cookieSecureEnv = Environment.GetEnvironmentVariable("COOKIE_SECURE");
        return cookieSecureEnv?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
            || (!_env.IsDevelopment() && cookieSecureEnv != "false");
    }

    private void SetOAuthStateCookie(string cookieName, string state)
    {
        Response.Cookies.Append(cookieName, state, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = IsSecureCookie(),
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
            Path = "/"
        });
    }

    private void ValidateOAuthState(string cookieName, string? stateFromQuery)
    {
        var stateFromCookie = Request.Cookies[cookieName];
        Response.Cookies.Delete(cookieName, new CookieOptions { Path = "/" });

        if (string.IsNullOrWhiteSpace(stateFromCookie) || stateFromCookie != stateFromQuery)
            throw new ExpectedException("Falha de validação do estado OAuth.");
    }

    private string GetGoogleRedirectUri()
    {
        var configured = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();
        return $"{Request.Scheme}://{Request.Host}/auth/google/callback";
    }

    private string GetMicrosoftRedirectUri()
    {
        var configured = Environment.GetEnvironmentVariable("MICROSOFT_REDIRECT_URI");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();
        return $"{Request.Scheme}://{Request.Host}/auth/microsoft/callback";
    }

    private static string GetFrontLoginUrl()
    {
        var configured = Environment.GetEnvironmentVariable("SSO_FRONTEND_LOGIN_URL")
            ?? Environment.GetEnvironmentVariable("GOOGLE_FRONTEND_LOGIN_URL")
            ?? Environment.GetEnvironmentVariable("MICROSOFT_FRONTEND_LOGIN_URL");

        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstOrigin = corsOrigins?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstOrigin))
            return $"{firstOrigin.TrimEnd('/')}/login";

        return "http://localhost:5173/login";
    }

    private static string BuildFrontLoginRedirect(string status, string? message = null, LoginResponse? login = null)
    {
        var baseUrl = GetFrontLoginUrl();
        var parameters = new List<string> { $"sso={Uri.EscapeDataString(status)}" };

        if (!string.IsNullOrWhiteSpace(message))
            parameters.Add($"message={Uri.EscapeDataString(message)}");

        if (login is not null)
        {
            parameters.Add($"name={Uri.EscapeDataString(login.name)}");
            parameters.Add($"email={Uri.EscapeDataString(login.email)}");
        }

        return $"{baseUrl}?{string.Join("&", parameters)}";
    }

    private static async Task<GoogleUserInfo> GetGoogleUserInfo(string authorizationCode, string clientId, string clientSecret, string redirectUri)
    {
        using var client = new HttpClient();

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", authorizationCode },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        });

        var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        if (!tokenResponse.IsSuccessStatusCode)
            throw new ExpectedException($"Falha ao obter token do Google: {tokenJson}", HttpStatusCode.BadGateway);

        using var tokenDoc = JsonDocument.Parse(tokenJson);
        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            throw new ExpectedException("Resposta do Google não contém access_token.", HttpStatusCode.BadGateway);

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ExpectedException("Access token inválido retornado pelo Google.", HttpStatusCode.BadGateway);

        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await client.SendAsync(userInfoRequest);
        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
        if (!userInfoResponse.IsSuccessStatusCode)
            throw new ExpectedException($"Falha ao obter perfil do Google: {userInfoJson}", HttpStatusCode.BadGateway);

        using var userInfoDoc = JsonDocument.Parse(userInfoJson);
        var email = userInfoDoc.RootElement.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        var name = userInfoDoc.RootElement.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        var emailVerified = userInfoDoc.RootElement.TryGetProperty("email_verified", out var verifiedEl) && verifiedEl.GetBoolean();

        return new GoogleUserInfo(email ?? string.Empty, name ?? string.Empty, emailVerified);
    }

    private static async Task<MicrosoftUserInfo> GetMicrosoftUserInfo(string authorizationCode, string clientId, string clientSecret, string tenantId, string redirectUri)
    {
        using var client = new HttpClient();
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", authorizationCode },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" },
            { "scope", "openid profile email User.Read" }
        });

        var tokenResponse = await client.PostAsync(tokenEndpoint, tokenRequest);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        if (!tokenResponse.IsSuccessStatusCode)
            throw new ExpectedException($"Falha ao obter token da Microsoft: {tokenJson}", HttpStatusCode.BadGateway);

        using var tokenDoc = JsonDocument.Parse(tokenJson);
        var idToken = tokenDoc.RootElement.TryGetProperty("id_token", out var idTokenEl) ? idTokenEl.GetString() : null;
        var accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenEl) ? accessTokenEl.GetString() : null;

        var email = string.Empty;
        var name = string.Empty;

        if (!string.IsNullOrWhiteSpace(idToken))
        {
            var payloadJson = DecodeJwtPayload(idToken);
            using var payloadDoc = JsonDocument.Parse(payloadJson);

            email = GetFirstString(payloadDoc.RootElement, "email", "preferred_username", "upn");
            name = GetFirstString(payloadDoc.RootElement, "name", "given_name");
        }

        if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(accessToken))
        {
            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/oidc/userinfo");
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userInfoResponse = await client.SendAsync(userInfoRequest);
            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();

            if (userInfoResponse.IsSuccessStatusCode)
            {
                using var userInfoDoc = JsonDocument.Parse(userInfoJson);
                email = GetFirstString(userInfoDoc.RootElement, "email", "preferred_username");
                if (string.IsNullOrWhiteSpace(name))
                    name = GetFirstString(userInfoDoc.RootElement, "name");
            }
        }

        if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(accessToken))
        {
            var meRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me?$select=displayName,mail,userPrincipalName");
            meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var meResponse = await client.SendAsync(meRequest);
            var meJson = await meResponse.Content.ReadAsStringAsync();

            if (meResponse.IsSuccessStatusCode)
            {
                using var meDoc = JsonDocument.Parse(meJson);
                email = GetFirstString(meDoc.RootElement, "mail", "userPrincipalName");
                if (string.IsNullOrWhiteSpace(name))
                    name = GetFirstString(meDoc.RootElement, "displayName");
            }
        }

        return new MicrosoftUserInfo(email ?? string.Empty, name ?? string.Empty);
    }

    private static string DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new ExpectedException("id_token inválido retornado pela Microsoft.", HttpStatusCode.BadGateway);

        var payload = parts[1].Replace('-', '+').Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string GetFirstString(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        return string.Empty;
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
public record GoogleUserInfo(string Email, string Name, bool EmailVerified);
public record MicrosoftUserInfo(string Email, string Name);
