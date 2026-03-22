using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

namespace server.Controllers;

[ApiController]
public class UserController : AuthenticatedController
{
    private readonly Context _context;
    private readonly INotification _notify;
    private readonly IWhatsAppNotification _whatsApp;
    private readonly IEmailNotification _email;

    public UserController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory,
        INotification notify,
        IWhatsAppNotification whatsApp,
        IEmailNotification email) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
        _notify = notify;
        _whatsApp = whatsApp;
        _email = email;
    }

    /// <summary>
    /// Busca as configurações do usuário autenticado
    /// </summary>
    [HttpGet("User/Settings")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult GetSettings()
    {
        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        return Ok(ToResponse(user));
    }

    /// <summary>
    /// Atualiza dados cadastrais e preferências de notificação do usuário autenticado
    /// </summary>
    [HttpPatch("User/Settings")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult UpdateSettings([FromBody] UserSettingsRequest request)
    {
        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var email = (request.email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        ValidateEmail(email);

        bool alreadyExists = _context.users
            .AsNoTracking()
            .Any(x => x.id != _user.id && x.email == email);
        if (alreadyExists)
            throw new ExpectedException("Já existe uma conta com esse email.", HttpStatusCode.Conflict);

        var phone = SanitizePhone(request.phone);
        if ((request.notify_whatsapp || request.notify_telegram) && string.IsNullOrWhiteSpace(phone))
            throw new ExpectedException("Informe um telefone com 11 dígitos para habilitar notificações por WhatsApp e Telegram.");

        user.email = email;
        user.phone = phone;
        user.notify_whatsapp = request.notify_whatsapp;
        user.notify_telegram = request.notify_telegram;
        user.notify_email = request.notify_email;
        user.calendar_public_enabled = request.calendar_public_enabled;
        if (user.calendar_public_enabled && user.calendar_public_token is null)
            user.calendar_public_token = Guid.NewGuid();

        if (!string.IsNullOrWhiteSpace(request.password))
        {
            var newPassword = request.password.Trim();
            if (newPassword.Length < 6)
                throw new ExpectedException("A senha deve ter pelo menos 6 caracteres.");
            user.password = newPassword;
        }

        _context.SaveChanges();

        return Ok(ToResponse(user));
    }

    [HttpPost("User/Settings/Totp/Generate")]
    [ProducesResponseType(typeof(TotpSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult GenerateTotpSetup()
    {
        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var secret = TotpUtility.GenerateBase32Secret();
        var uri = TotpUtility.BuildOtpAuthUri("RentaTop", user.email, secret);

        return Ok(new TotpSetupResponse(secret, uri, user.email));
    }

    [HttpPost("User/Settings/Totp/Enable")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult EnableTotp([FromBody] TotpEnableRequest request)
    {
        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (string.IsNullOrWhiteSpace(request.secret))
            throw new ExpectedException("Secret TOTP é obrigatório.");

        if (!TotpUtility.ValidateCode(request.secret, request.code))
            throw new ExpectedException("Código TOTP inválido.");

        user.totp_secret = request.secret.Trim().ToUpperInvariant();
        user.totp_enabled = true;
        _context.SaveChanges();

        return Ok(ToResponse(user));
    }

    [HttpPost("User/Settings/Totp/Disable")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult DisableTotp([FromBody] TotpDisableRequest request)
    {
        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (!user.totp_enabled)
            return Ok(ToResponse(user));

        if (string.IsNullOrWhiteSpace(user.totp_secret) ||
            !TotpUtility.ValidateCode(user.totp_secret, request.code))
        {
            throw new ExpectedException("Código TOTP inválido.");
        }

        user.totp_enabled = false;
        user.totp_secret = null;
        _context.SaveChanges();

        return Ok(ToResponse(user));
    }

    /// <summary>
    /// Envia uma mensagem de teste para o canal de Telegram configurado
    /// </summary>
    [HttpPost("User/Settings/TestTelegram")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TestTelegram([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] NotificationTestRequest? request = null)
    {
        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var message = $"✅ Mensagem de teste enviada em {now}.{Environment.NewLine}Usuário: {user.name}";

        try
        {
            await _notify.Notify("📈 RentaTop | Teste Telegram", message);
        }
        catch (Exception ex)
        {
            throw new ExpectedException($"Falha ao enviar mensagem no Telegram: {ex.Message}", HttpStatusCode.BadGateway);
        }

        return Ok(new GenericMessageResponse("Mensagem de teste enviada no Telegram."));
    }

    /// <summary>
    /// Envia uma mensagem de teste para o WhatsApp informado no cadastro do usuário
    /// </summary>
    [HttpPost("User/Settings/TestWhatsApp")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TestWhatsApp([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] NotificationTestRequest? request = null)
    {
        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var phone = string.IsNullOrWhiteSpace(request?.phone)
            ? SanitizePhone(user.phone)
            : SanitizePhone(request.phone);
        if (string.IsNullOrWhiteSpace(phone))
            throw new ExpectedException("Informe um telefone válido para testar o WhatsApp.");

        var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var message = $"📈 RentaTop{Environment.NewLine}✅ Mensagem de teste enviada em {now}.{Environment.NewLine}Usuário: {user.name}";

        try
        {
            await _whatsApp.Notify(phone, "📈 RentaTop | Teste WhatsApp", message);
        }
        catch (Exception ex)
        {
            throw new ExpectedException($"Falha ao enviar mensagem no WhatsApp: {ex.Message}", HttpStatusCode.BadGateway);
        }

        return Ok(new GenericMessageResponse("Mensagem de teste enviada no WhatsApp."));
    }

    /// <summary>
    /// Envia uma mensagem de teste para o email informado no cadastro do usuário
    /// </summary>
    [HttpPost("User/Settings/TestEmail")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TestEmail()
    {
        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (!user.notify_email)
            throw new ExpectedException("Ative as notificações por Email nas configurações para testar o envio.");

        ValidateEmail(user.email);

        var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var message = $"📈 RentaTop{Environment.NewLine}✅ Mensagem de teste enviada em {now}.{Environment.NewLine}Usuário: {user.name}";
        try
        {
            await _email.Notify(user.email, "📈 RentaTop | Teste Email", message);
        }
        catch (Exception ex)
        {
            throw new ExpectedException($"Falha ao enviar email de teste: {ex.Message}", HttpStatusCode.BadGateway);
        }

        return Ok(new GenericMessageResponse("Mensagem de teste enviada por Email."));
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

    private static string SanitizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
            throw new ExpectedException("Telefone deve ter 11 dígitos no formato 99999999999.");

        return digits;
    }

    private UserSettingsResponse ToResponse(User user) =>
        new UserSettingsResponse(
            user.name,
            user.email,
            user.phone ?? string.Empty,
            user.cpf ?? string.Empty,
            user.notify_whatsapp,
            user.notify_telegram,
            user.notify_email,
            user.calendar_public_enabled,
            user.calendar_public_enabled ? BuildPublicCalendarUrl(user.calendar_public_token) : null,
            user.totp_enabled
        );

    private string? BuildPublicCalendarUrl(Guid? token)
    {
        if (token is null) return null;

        var publicApiUrl = Environment.GetEnvironmentVariable("PUBLIC_API_URL")?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(publicApiUrl))
            return $"{publicApiUrl}/public/calendar/{token}.ics";

        return $"{Request.Scheme}://{Request.Host}/public/calendar/{token}.ics";
    }
}

public record UserSettingsRequest(
    string email,
    string? password,
    string? phone,
    bool notify_whatsapp,
    bool notify_telegram,
    bool notify_email,
    bool calendar_public_enabled,
    bool? totp_enabled = null
);

public record NotificationTestRequest(
    string? phone
);

public record UserSettingsResponse(
    string name,
    string email,
    string phone,
    string cpf,
    bool notify_whatsapp,
    bool notify_telegram,
    bool notify_email,
    bool calendar_public_enabled,
    string? calendar_public_url,
    bool totp_enabled
);

public record TotpSetupResponse(
    string secret,
    string otpauth_uri,
    string account
);

public record TotpEnableRequest(
    string secret,
    string code
);

public record TotpDisableRequest(
    string code
);

public record GenericMessageResponse(
    string message
);
