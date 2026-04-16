using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
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
    private const int EmailVerificationDigits = 6;
    private const int EmailVerificationPeriodSeconds = 300;
    private const int EmailVerificationAllowedDriftSteps = 1;
    private static readonly TimeSpan EmailVerificationResendInterval = TimeSpan.FromSeconds(60);

    private readonly Context _context;
    private readonly IDatabase _redis;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailNotification _email;
    private readonly string? _clientBaseUrl;

    public LoginController(
        IDbContextFactory<Context> contextFactory,
        IConnectionMultiplexer muxer_redis,
        IWebHostEnvironment env,
        IEmailNotification email)
    {
        _context = contextFactory.CreateDbContext();
        _redis = muxer_redis.GetDatabase();
        _env = env;
        _email = email;
        _clientBaseUrl = Environment.GetEnvironmentVariable("BASE_URL_CLIENT");
    }

    /// <summary>
    /// Realiza o processo de login e define o cookie JWT HttpOnly
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginStartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRecord credentials)
    {
        var email = credentials.email?.Trim().ToLowerInvariant() ?? string.Empty;

        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.email == email);
        if (user is null || !user.CheckPass(credentials.password))
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta", HttpStatusCode.Unauthorized);

        if (!user.email_verified)
            throw new ExpectedException("Sua conta ainda não foi ativada. Verifique o código enviado para seu email antes de entrar.", HttpStatusCode.Forbidden);

        if (user.totp_enabled)
        {
            if (string.IsNullOrWhiteSpace(user.totp_secret))
                throw new ExpectedException("TOTP habilitado sem chave secreta configurada para esta conta.", HttpStatusCode.Unauthorized);

            var challengeId = Guid.NewGuid().ToString("N");
            _redis.StringSet(GetTotpChallengeKey(challengeId), user.id.ToString(), TimeSpan.FromMinutes(5));
            return Ok(new LoginStartResponse(true, challengeId, null, null, null));
        }

        var login = SetSession(user);
        return Ok(new LoginStartResponse(false, null, login.name, login.email, login.user_type));
    }

    [HttpPost("login/totp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult LoginTotp([FromBody] TotpLoginRequest request)
    {
        var challengeId = request.challenge_id?.Trim() ?? string.Empty;
        var code = request.code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(challengeId))
            throw new ExpectedException("Desafio de login TOTP ausente.", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(code))
            throw new ExpectedException("Código TOTP é obrigatório.", HttpStatusCode.BadRequest);

        var challengeKey = GetTotpChallengeKey(challengeId);
        var userIdValue = _redis.StringGet(challengeKey);

        if (userIdValue.IsNullOrEmpty)
            throw new ExpectedException("Desafio TOTP expirado. Faça login novamente.", HttpStatusCode.Unauthorized);

        if (!Guid.TryParse(userIdValue.ToString(), out var userId))
        {
            _redis.KeyDelete(challengeKey);
            throw new ExpectedException("Desafio TOTP inválido. Faça login novamente.", HttpStatusCode.Unauthorized);
        }

        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == userId);
        if (user is null || !user.totp_enabled || string.IsNullOrWhiteSpace(user.totp_secret))
        {
            _redis.KeyDelete(challengeKey);
            throw new ExpectedException("Não foi possível validar o TOTP para esta conta.", HttpStatusCode.Unauthorized);
        }

        if (!TotpUtility.ValidateCode(user.totp_secret, code))
            throw new ExpectedException("Código TOTP inválido.", HttpStatusCode.Unauthorized);

        _redis.KeyDelete(challengeKey);
        return CreateSession(user);
    }

    /// <summary>
    /// Cria uma nova conta pendente e envia o código de verificação por email
    /// </summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SignupPendingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Signup([FromBody] SignUpRequest request)
    {
        var name = request.name?.Trim() ?? string.Empty;
        var email = request.email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            throw new ExpectedException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        ValidateEmail(email);

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ExpectedException("A senha deve ter pelo menos 6 caracteres.");

        var existingUser = _context.users.FirstOrDefault(x => x.email == email || x.pending_email == email);
        if (existingUser is not null)
        {
            if (existingUser.email != email)
                throw new ExpectedException("Este email está reservado por uma alteração pendente em outra conta. Tente outro email.", HttpStatusCode.Conflict);

            if (existingUser.email_verified)
                throw new ExpectedException("Já existe uma conta com esse email.", HttpStatusCode.Conflict);

            throw new ExpectedException("Já existe um cadastro pendente para esse email. Informe o código recebido ou solicite um novo envio.", HttpStatusCode.Conflict);
        }

        User user = new User
        {
            id = SnowflakeGuid.NewGuid(),
            name = name,
            email = email,
            password = password,
            user_type = UserType.Common,
            auth_provider = AuthProvider.Password,
            email_verified = false,
            email_verification_secret = TotpUtility.GenerateBase32Secret(),
            email_verification_sent_at = DateTime.UtcNow
        };

        _context.users.Add(user);
        _context.SaveChanges();

        try
        {
            await SendSignupVerificationEmail(user);
            return Ok(new SignupPendingResponse(
                "Enviamos um código de verificação para seu email. Informe-o para ativar a conta.",
                user.email,
                true));
        }
        catch
        {
            return Ok(new SignupPendingResponse(
                "Sua conta foi criada, mas não conseguimos enviar o código agora. Solicite um novo envio para ativá-la.",
                user.email,
                false));
        }
    }

    [HttpPost("signup/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult VerifySignup([FromBody] SignupVerificationRequest request)
    {
        var email = (request.email ?? string.Empty).Trim().ToLowerInvariant();
        var code = request.code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        if (string.IsNullOrWhiteSpace(code))
            throw new ExpectedException("Código de verificação é obrigatório.");

        var user = _context.users.FirstOrDefault(x => x.email == email);
        if (user is null || user.email_verified)
            throw new ExpectedException("Cadastro pendente não encontrado para esse email.", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(user.email_verification_secret) ||
            !TotpUtility.ValidateCode(
                user.email_verification_secret,
                code,
                allowedDriftSteps: EmailVerificationAllowedDriftSteps,
                periodSeconds: EmailVerificationPeriodSeconds,
                digits: EmailVerificationDigits))
        {
            throw new ExpectedException("Código de verificação inválido ou expirado.", HttpStatusCode.Unauthorized);
        }

        user.email_verified = true;
        user.email_verification_secret = null;
        user.email_verification_sent_at = null;
        _context.SaveChanges();

        return CreateSession(user);
    }

    [HttpPost("signup/verification/resend")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendSignupVerification([FromBody] SignupVerificationResendRequest request)
    {
        var email = (request.email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        ValidateEmail(email);

        var user = _context.users.FirstOrDefault(x => x.email == email);
        if (user is null)
            throw new ExpectedException("Cadastro pendente não encontrado para esse email.", HttpStatusCode.NotFound);

        if (user.email_verified)
            throw new ExpectedException("Esta conta já está ativa. Faça login normalmente.", HttpStatusCode.Conflict);

        if (user.email_verification_sent_at.HasValue &&
            DateTime.UtcNow - user.email_verification_sent_at.Value < EmailVerificationResendInterval)
        {
            throw new ExpectedException("Aguarde 60 segundos antes de solicitar um novo código.");
        }

        user.email_verification_secret = TotpUtility.GenerateBase32Secret();
        user.email_verification_sent_at = DateTime.UtcNow;
        _context.SaveChanges();

        try
        {
            await SendSignupVerificationEmail(user);
        }
        catch (Exception ex)
        {
            throw new ExpectedException($"Falha ao enviar email de verificação: {ex.Message}", HttpStatusCode.BadGateway);
        }

        return Ok(new PasswordResetRequestResponse("Novo código de verificação enviado para seu email."));
    }

    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        var email = (request.email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        ValidateEmail(email);

        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.email == email);
        if (user is not null)
        {
            var token = GeneratePasswordResetToken(user);
            var resetUrl = BuildPasswordResetUrl(token);
            var expiresAt = DateTime.Now.AddHours(1).ToString("dd/MM/yyyy HH:mm");
            var message =
$@"Olá, {user.name}.

Recebemos uma solicitação para redefinir sua senha no RendaTop.

Use o link abaixo para cadastrar uma nova senha:
{resetUrl}

Esse link expira em 1 hora ({expiresAt}).

Se você não solicitou essa alteração, ignore este email.";

            try
            {
                await _email.Notify(user.email, "RendaTop | Redefinição de senha", message);
            }
            catch (Exception ex)
            {
                throw new ExpectedException($"Falha ao enviar email de redefinição de senha: {ex.Message}", HttpStatusCode.BadGateway);
            }
        }

        return Ok(new PasswordResetRequestResponse("Se existir uma conta com esse email, enviaremos um link de redefinição válido por 1 hora."));
    }

    [HttpPost("password-reset/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult ConfirmPasswordReset([FromBody] PasswordResetConfirmRequest request)
    {
        var token = (request.token ?? string.Empty).Trim();
        var password = request.password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
            throw new ExpectedException("Token de redefinição é obrigatório.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ExpectedException("A senha deve ter pelo menos 6 caracteres.");

        var payload = ValidatePasswordResetToken(token);
        var user = _context.users.FirstOrDefault(x => x.id == payload.UserId && x.email == payload.Email);
        if (user is null)
            throw new ExpectedException("Link de redefinição inválido.", HttpStatusCode.BadRequest);

        user.password = password;
        _context.SaveChanges();

        return Ok(new PasswordResetRequestResponse("Senha redefinida com sucesso."));
    }

    [HttpPost("totp-reset/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestTotpReset([FromBody] TotpResetRequest request)
    {
        var email = (request.email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        ValidateEmail(email);

        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.email == email);
        if (user is not null && user.totp_enabled)
        {
            var token = GenerateActionToken(user, "totp-reset");
            var resetUrl = BuildTotpResetUrl(token);
            var expiresAt = DateTime.Now.AddHours(1).ToString("dd/MM/yyyy HH:mm");
            var message =
$@"Olá, {user.name}.

Recebemos uma solicitação para remover a autenticação em duas etapas (TOTP) da sua conta no RendaTop.

Use o link abaixo para confirmar a remoção:
{resetUrl}

Esse link expira em 1 hora ({expiresAt}).

Se você não solicitou essa alteração, ignore este email.";

            try
            {
                await _email.Notify(user.email, "RendaTop | Remoção do TOTP", message);
            }
            catch (Exception ex)
            {
                throw new ExpectedException($"Falha ao enviar email para remoção do TOTP: {ex.Message}", HttpStatusCode.BadGateway);
            }
        }

        return Ok(new PasswordResetRequestResponse("Se existir uma conta com TOTP ativo nesse email, enviaremos um link para remover a autenticação em duas etapas válido por 1 hora."));
    }

    [HttpPost("totp-reset/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public IActionResult ConfirmTotpReset([FromBody] TotpResetConfirmRequest request)
    {
        var token = (request.token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new ExpectedException("Token de redefinição do TOTP é obrigatório.");

        var payload = ValidateActionToken(token, "totp-reset");
        var user = _context.users.FirstOrDefault(x => x.id == payload.UserId && x.email == payload.Email);
        if (user is null)
            throw new ExpectedException("Link de redefinição do TOTP inválido.", HttpStatusCode.BadRequest);

        user.totp_enabled = false;
        user.totp_secret = null;
        _context.SaveChanges();

        return Ok(new PasswordResetRequestResponse("Autenticação em duas etapas removida com sucesso."));
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

            var login = EnsureUserAndCreateSession(userInfo.Email, userInfo.Name, AuthProvider.Google);
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

            var login = EnsureUserAndCreateSession(userInfo.Email, userInfo.Name, AuthProvider.Microsoft);
            return Redirect(BuildFrontLoginRedirect("microsoft_success", null, login));
        }
        catch (Exception ex)
        {
            return Redirect(BuildFrontLoginRedirect("microsoft_error", ex.Message));
        }
    }

    private IActionResult CreateSession(User user) => Ok(SetSession(user));

    private static string GetTotpChallengeKey(string challengeId) => $"login:totp:challenge:{challengeId}";

    private LoginResponse EnsureUserAndCreateSession(string emailInput, string? nameInput, AuthProvider authProvider)
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
                password = Guid.NewGuid().ToString("N"),
                user_type = UserType.Common,
                auth_provider = authProvider,
                email_verified = true
            };
            _context.users.Add(user);
            _context.SaveChanges();
        }
        else
        {
            var changed = false;

            if (user.auth_provider != authProvider)
            {
                user.auth_provider = authProvider;
                changed = true;
            }

            if (!user.email_verified || !string.IsNullOrWhiteSpace(user.email_verification_secret))
            {
                user.email_verified = true;
                user.email_verification_secret = null;
                user.email_verification_sent_at = null;
                changed = true;
            }

            if (changed)
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

        return new LoginResponse(user.name, user.email, user.user_type);
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

    private string BuildPasswordResetUrl(string token)
    {
        var configured = Environment.GetEnvironmentVariable("PASSWORD_RESET_FRONTEND_URL");
        if (!string.IsNullOrWhiteSpace(configured))
            return $"{configured.Trim()}?token={Uri.EscapeDataString(token)}";

        return BuildFrontActionUrl("/reset-password", token);
    }

    private string BuildTotpResetUrl(string token)
    {
        var configured = Environment.GetEnvironmentVariable("TOTP_RESET_FRONTEND_URL");
        if (!string.IsNullOrWhiteSpace(configured))
            return $"{configured.Trim()}?token={Uri.EscapeDataString(token)}";

        return BuildFrontActionUrl("/reset-totp", token);
    }

    private static string BuildFrontActionUrl(string path, string token)
    {
        var loginUrl = GetFrontLoginUrl();
        const string loginSuffix = "/login";
        var actionUrl = loginUrl.EndsWith(loginSuffix, StringComparison.OrdinalIgnoreCase)
            ? $"{loginUrl[..^loginSuffix.Length]}{path}"
            : $"{loginUrl.TrimEnd('/')}{path}";

        return $"{actionUrl}?token={Uri.EscapeDataString(token)}";
    }

    private async Task SendSignupVerificationEmail(User user)
    {
        if (string.IsNullOrWhiteSpace(user.email_verification_secret))
            throw new ExpectedException("Código de verificação indisponível para esta conta.", HttpStatusCode.BadRequest);

        var code = TotpUtility.GenerateCode(
            user.email_verification_secret,
            periodSeconds: EmailVerificationPeriodSeconds,
            digits: EmailVerificationDigits);

        var message = EmailVerificationEmailTemplate.BuildSignup(user, code, _clientBaseUrl);

        await _email.Notify(user.email, "RendaTop | Verificação de email", message, isHtml: true);
    }

    private static void ValidateEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
        }
        catch
        {
            throw new ExpectedException("Email inválido.");
        }
    }

    private static string GeneratePasswordResetToken(User user)
        => GenerateActionToken(user, "password-reset");

    private static string GenerateActionToken(User user, string purpose)
    {
        var payload = new ActionTokenPayload(
            user.id,
            user.email,
            DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            purpose);

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadPart = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signaturePart = SignResetPayload(payloadPart);
        return $"{payloadPart}.{signaturePart}";
    }

    private static ActionTokenPayload ValidatePasswordResetToken(string token)
        => ValidateActionToken(token, "password-reset");

    private static ActionTokenPayload ValidateActionToken(string token, string purpose)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
            throw new ExpectedException("Link de redefinição inválido.", HttpStatusCode.BadRequest);

        var payloadPart = parts[0];
        var providedSignature = parts[1];
        var expectedSignature = SignResetPayload(payloadPart);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedSignature),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            throw new ExpectedException("Link de redefinição inválido.", HttpStatusCode.BadRequest);
        }

        ActionTokenPayload? payload;
        try
        {
            var payloadJson = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(payloadPart));
            payload = JsonSerializer.Deserialize<ActionTokenPayload>(payloadJson);
        }
        catch
        {
            throw new ExpectedException("Link de redefinição inválido.", HttpStatusCode.BadRequest);
        }

        if (payload is null ||
            payload.Purpose != purpose ||
            payload.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            throw new ExpectedException("Link de redefinição expirado ou inválido.", HttpStatusCode.BadRequest);
        }

        return payload;
    }

    private static string SignResetPayload(string payloadPart)
    {
        var secret = Environment.GetEnvironmentVariable("VITE_JWT_KEY");
        if (string.IsNullOrWhiteSpace(secret))
            throw new ExpectedException("VITE_JWT_KEY não configurado no servidor.", HttpStatusCode.InternalServerError);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart));
        return WebEncoders.Base64UrlEncode(signatureBytes);
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
            parameters.Add($"user_type={Uri.EscapeDataString(login.user_type.ToString())}");
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

public record LoginResponse(string name, string email, UserType user_type);
public record LoginStartResponse(bool requires_totp, string? challenge_id, string? name, string? email, UserType? user_type);
public record TotpLoginRequest(string challenge_id, string code);
public record SignUpRequest(string name, string email, string password);
public record SignupPendingResponse(string message, string email, bool email_sent);
public record SignupVerificationRequest(string email, string code);
public record SignupVerificationResendRequest(string email);
public record PasswordResetRequest(string email);
public record PasswordResetConfirmRequest(string token, string password);
public record TotpResetRequest(string email);
public record TotpResetConfirmRequest(string token);
public record PasswordResetRequestResponse(string message);
public record ActionTokenPayload(Guid UserId, string Email, long Exp, string Purpose);
public record GoogleUserInfo(string Email, string Name, bool EmailVerified);
public record MicrosoftUserInfo(string Email, string Name);
