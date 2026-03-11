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
        Context context,
        INotification notify,
        IWhatsAppNotification whatsApp,
        IEmailNotification email) : base(httpContextAccessor)
    {
        _context = context;
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

        user.email = email;
        user.phone = phone;
        user.notify_whatsapp = request.notify_whatsapp;
        user.notify_telegram = request.notify_telegram;
        user.notify_email = request.notify_email;

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

    /// <summary>
    /// Envia uma mensagem de teste para o canal de Telegram configurado
    /// </summary>
    [HttpPost("User/Settings/TestTelegram")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TestTelegram()
    {
        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (!user.notify_telegram)
            throw new ExpectedException("Ative as notificações por Telegram nas configurações para testar o envio.");

        var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var message = $"Mensagem de teste enviada em {now}.{Environment.NewLine}Usuário: {user.name}";

        try
        {
            await _notify.Notify("Teste Telegram", message);
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
    public async Task<IActionResult> TestWhatsApp()
    {
        var user = _context.users.AsNoTracking().FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (!user.notify_whatsapp)
            throw new ExpectedException("Ative as notificações por WhatsApp nas configurações para testar o envio.");

        var phone = SanitizePhone(user.phone);
        if (string.IsNullOrWhiteSpace(phone))
            throw new ExpectedException("Informe um telefone válido para testar o WhatsApp.");

        var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var message = $"Mensagem de teste enviada em {now}.{Environment.NewLine}Usuário: {user.name}";

        try
        {
            await _whatsApp.Notify(phone, "Teste WhatsApp", message);
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
        var message = $"Mensagem de teste enviada em {now}.{Environment.NewLine}Usuário: {user.name}";

        try
        {
            await _email.Notify(user.email, "Teste Email", message);
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

    private static UserSettingsResponse ToResponse(User user) =>
        new UserSettingsResponse(
            user.name,
            user.email,
            user.phone ?? string.Empty,
            user.notify_whatsapp,
            user.notify_telegram,
            user.notify_email
        );
}

public record UserSettingsRequest(
    string email,
    string? password,
    string? phone,
    bool notify_whatsapp,
    bool notify_telegram,
    bool notify_email
);

public record UserSettingsResponse(
    string name,
    string email,
    string phone,
    bool notify_whatsapp,
    bool notify_telegram,
    bool notify_email
);

public record GenericMessageResponse(
    string message
);
