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
    private const string TestEmailDestination = "hudsonventura@gmail.com";
    private const string DeleteAccountConfirmationText = "EXCLUIR";
    private const int EmailVerificationDigits = 6;
    private const int EmailVerificationPeriodSeconds = 300;
    private const int EmailVerificationAllowedDriftSteps = 1;
    private static readonly TimeSpan EmailVerificationResendInterval = TimeSpan.FromSeconds(60);
    private readonly Context _context;
    private readonly INotification _notify;
    private readonly IWhatsAppNotification _whatsApp;
    private readonly IEmailNotification _email;
    private readonly IBrowserPushNotification _browserPush;
    private readonly string? _clientBaseUrl;

    public UserController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory,
        INotification notify,
        IWhatsAppNotification whatsApp,
        IEmailNotification email,
        IBrowserPushNotification browserPush) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
        _notify = notify;
        _whatsApp = whatsApp;
        _email = email;
        _browserPush = browserPush;
        _clientBaseUrl = Environment.GetEnvironmentVariable("BASE_URL_CLIENT");
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
    public async Task<IActionResult> UpdateSettings([FromBody] UserSettingsRequest request)
    {
        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var name = (request.name ?? string.Empty).Trim();
        var email = (request.email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name))
            throw new ExpectedException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ExpectedException("Email é obrigatório.");

        ValidateEmail(email);

        var phone = SanitizePhone(request.phone);
        if (request.notify_whatsapp && string.IsNullOrWhiteSpace(phone))
            throw new ExpectedException("Informe um telefone com 11 dígitos para habilitar notificações por WhatsApp.");

        var telegramChatId = SanitizeTelegramChatId(request.telegram_chat_id);
        if (request.notify_telegram && string.IsNullOrWhiteSpace(telegramChatId))
            throw new ExpectedException("Informe o Chat ID do Telegram para habilitar notificações por Telegram.");

        var canUseWhatsAppNotifications = CanUseWhatsAppNotifications(user.id);
        var canUseCalendarIcs = CanUseCalendarIcs(user.id);

        if (request.notify_whatsapp && !canUseWhatsAppNotifications)
            throw new ExpectedException("Notificações por WhatsApp exigem um plano ativo que tenha esse recurso liberado.");

        if (request.calendar_public_enabled && !canUseCalendarIcs)
            throw new ExpectedException("Compartilhamento público de calendário ICS exige um plano ativo que tenha esse recurso liberado.");

        user.name = name;
        user.phone = phone;
        user.notify_whatsapp = request.notify_whatsapp;
        user.notify_telegram = request.notify_telegram;
        user.telegram_chat_id = telegramChatId;
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

        var pendingEmailVerificationSent = false;

        if (email == user.email)
        {
            ClearPendingEmailChange(user);
        }
        else
        {
            EnsureEmailAvailable(email, user.id);

            if (!string.Equals(user.pending_email, email, StringComparison.Ordinal))
            {
                user.pending_email = email;
                user.pending_email_verification_secret = TotpUtility.GenerateBase32Secret();
                user.pending_email_verification_sent_at = DateTime.UtcNow;
                pendingEmailVerificationSent = true;
            }
            else if (string.IsNullOrWhiteSpace(user.pending_email_verification_secret))
            {
                user.pending_email_verification_secret = TotpUtility.GenerateBase32Secret();
                pendingEmailVerificationSent = true;
            }
        }

        _context.SaveChanges();

        if (pendingEmailVerificationSent)
        {
            try
            {
                await SendPendingEmailVerificationEmail(user);
            }
            catch
            {
                return Ok(ToResponse(user, pendingEmailVerificationSent: false));
            }
        }

        return Ok(ToResponse(
            user,
            pendingEmailVerificationSent: pendingEmailVerificationSent ? true : null));
    }

    [HttpPost("User/Settings/Email/Verify")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult VerifyPendingEmail([FromBody] PendingEmailVerificationRequest request)
    {
        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (string.IsNullOrWhiteSpace(request.code))
            throw new ExpectedException("Código de verificação é obrigatório.");

        if (string.IsNullOrWhiteSpace(user.pending_email))
            throw new ExpectedException("Não existe alteração de email pendente para esta conta.", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(user.pending_email_verification_secret) ||
            !TotpUtility.ValidateCode(
                user.pending_email_verification_secret,
                request.code.Trim(),
                allowedDriftSteps: EmailVerificationAllowedDriftSteps,
                periodSeconds: EmailVerificationPeriodSeconds,
                digits: EmailVerificationDigits))
        {
            throw new ExpectedException("Código de verificação inválido ou expirado.", HttpStatusCode.Unauthorized);
        }

        EnsureEmailAvailable(user.pending_email, user.id);

        user.email = user.pending_email;
        ClearPendingEmailChange(user);
        _context.SaveChanges();

        return Ok(ToResponse(user));
    }

    [HttpPost("User/Settings/Email/Resend")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResendPendingEmailVerification()
    {
        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (string.IsNullOrWhiteSpace(user.pending_email))
            throw new ExpectedException("Não existe alteração de email pendente para esta conta.", HttpStatusCode.BadRequest);

        EnsureEmailAvailable(user.pending_email, user.id);

        if (user.pending_email_verification_sent_at.HasValue &&
            DateTime.UtcNow - user.pending_email_verification_sent_at.Value < EmailVerificationResendInterval)
        {
            throw new ExpectedException("Aguarde 60 segundos antes de solicitar um novo código.");
        }

        user.pending_email_verification_secret = TotpUtility.GenerateBase32Secret();
        user.pending_email_verification_sent_at = DateTime.UtcNow;
        _context.SaveChanges();

        try
        {
            await SendPendingEmailVerificationEmail(user);
        }
        catch (Exception ex)
        {
            throw new ExpectedException($"Falha ao enviar email de verificação: {ex.Message}", HttpStatusCode.BadGateway);
        }

        return Ok(new GenericMessageResponse("Novo código de verificação enviado para seu novo email."));
    }

    [HttpPost("User/Settings/Email/Cancel")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult CancelPendingEmailVerification()
    {
        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (string.IsNullOrWhiteSpace(user.pending_email))
            throw new ExpectedException("Não existe alteração de email pendente para esta conta.", HttpStatusCode.BadRequest);

        ClearPendingEmailChange(user);
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

        var telegramChatId = string.IsNullOrWhiteSpace(request?.telegram_chat_id)
            ? SanitizeTelegramChatId(user.telegram_chat_id)
            : SanitizeTelegramChatId(request?.telegram_chat_id);
        if (string.IsNullOrWhiteSpace(telegramChatId))
            throw new ExpectedException("Informe o Chat ID do Telegram no campo ao lado de Telegram e clique em testar novamente. Você pode testar antes mesmo de salvar as configurações.");

        try
        {
            await _notify.Notify("📈 RentaTop | Teste Telegram", message, telegramChatId);
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

        if (!CanUseWhatsAppNotifications(user.id))
            throw new ExpectedException("Notificações por WhatsApp exigem um plano ativo que tenha esse recurso liberado.");

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

        
        ValidateEmail(TestEmailDestination);

        var message = TestEmailTemplate.Build(user, _clientBaseUrl);
        try
        {
            await _email.Notify(TestEmailDestination, "📈 RentaTop | Teste Email", message, isHtml: true);
        }
        catch (Exception ex)
        {
            throw new ExpectedException($"Falha ao enviar email de teste: {ex.Message}", HttpStatusCode.BadGateway);
        }

        return Ok(new GenericMessageResponse("Mensagem de teste enviada por Email."));
    }

    [HttpGet("User/Settings/BrowserPush/PublicKey")]
    [ProducesResponseType(typeof(BrowserPushPublicKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult GetBrowserPushPublicKey()
    {
        return Ok(new BrowserPushPublicKeyResponse(
            _browserPush.IsConfigured,
            _browserPush.IsConfigured ? _browserPush.PublicKey : null
        ));
    }

    [HttpPost("User/Settings/BrowserPush/Subscribe")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult SubscribeBrowserPush([FromBody] BrowserPushSubscriptionRequest request)
    {
        if (!_browserPush.IsConfigured)
            throw new ExpectedException("Notificações no navegador não estão configuradas no servidor.");

        if (string.IsNullOrWhiteSpace(request.endpoint) ||
            string.IsNullOrWhiteSpace(request.p256dh) ||
            string.IsNullOrWhiteSpace(request.auth))
        {
            throw new ExpectedException("Inscrição do navegador inválida.");
        }

        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var endpoint = request.endpoint.Trim();
        var subscription = _context.browser_push_subscriptions
            .FirstOrDefault(x => x.endpoint == endpoint);

        if (subscription is null)
        {
            subscription = new BrowserPushSubscription
            {
                user_id = user.id,
                endpoint = endpoint,
                p256dh = request.p256dh.Trim(),
                auth = request.auth.Trim(),
                user_agent = request.user_agent?.Trim(),
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.browser_push_subscriptions.Add(subscription);
        }
        else
        {
            subscription.user_id = user.id;
            subscription.p256dh = request.p256dh.Trim();
            subscription.auth = request.auth.Trim();
            subscription.user_agent = request.user_agent?.Trim();
            subscription.updated_at = DateTime.UtcNow;
        }

        user.notify_browser = true;
        _context.SaveChanges();

        return Ok(new GenericMessageResponse("Notificações do navegador habilitadas neste dispositivo."));
    }

    [HttpPost("User/Settings/BrowserPush/Unsubscribe")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public IActionResult UnsubscribeBrowserPush([FromBody] BrowserPushUnsubscribeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.endpoint))
            throw new ExpectedException("Endpoint da inscrição do navegador é obrigatório.");

        var user = _context.users.FirstOrDefault(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        var endpoint = request.endpoint.Trim();
        var subscriptions = _context.browser_push_subscriptions
            .Where(x => x.user_id == user.id && x.endpoint == endpoint)
            .ToList();

        if (subscriptions.Count > 0)
            _context.browser_push_subscriptions.RemoveRange(subscriptions);

        var hasAnySubscription = _context.browser_push_subscriptions
            .AsNoTracking()
            .Any(x => x.user_id == user.id && x.endpoint != endpoint);

        if (!hasAnySubscription)
            user.notify_browser = false;

        _context.SaveChanges();

        return Ok(new GenericMessageResponse("Notificações do navegador desabilitadas neste dispositivo."));
    }

    [HttpDelete("User/Settings/DeleteAccount")]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteOwnAccount([FromBody] DeleteOwnAccountRequest request)
    {
        var user = await _context.users.FirstOrDefaultAsync(x => x.id == _user.id);
        if (user is null)
            throw new ExpectedException("Usuário não encontrado.", HttpStatusCode.NotFound);

        if (!request.confirm_first_step || !request.confirm_second_step)
            throw new ExpectedException("Confirmação de exclusão incompleta.");

        if (!string.Equals(
                (request.confirmation_text ?? string.Empty).Trim(),
                DeleteAccountConfirmationText,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ExpectedException($"Digite {DeleteAccountConfirmationText} para confirmar a exclusão da conta.");
        }

        await DeleteUserAccountAsync(user);

        HttpContext?.Response?.Cookies.Delete("jwt", new CookieOptions { Path = "/" });

        return Ok(new GenericMessageResponse("Sua conta foi excluída permanentemente."));
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

    private UserSettingsResponse ToResponse(User user, bool? pendingEmailVerificationSent = null)
    {
        var whatsappNotificationsEnabled = CanUseWhatsAppNotifications(user.id);
        var calendarIcsEnabled = CanUseCalendarIcs(user.id);
        var aiDocumentExtraction = GetAiDocumentExtractionAccess(user.id);

        return new UserSettingsResponse(
            user.name,
            user.email,
            user.phone ?? string.Empty,
            user.cpf ?? string.Empty,
            whatsappNotificationsEnabled && user.notify_whatsapp,
            user.notify_telegram,
            user.telegram_chat_id,
            user.notify_email,
            user.notify_browser,
            calendarIcsEnabled && user.calendar_public_enabled,
            calendarIcsEnabled && user.calendar_public_enabled
                ? BuildPublicCalendarUrl(user.calendar_public_token)
                : null,
            user.totp_enabled,
            whatsappNotificationsEnabled,
            calendarIcsEnabled,
            aiDocumentExtraction.enabled,
            aiDocumentExtraction.current_usage,
            aiDocumentExtraction.monthly_limit,
            aiDocumentExtraction.restriction_message,
            user.user_type,
            user.pending_email,
            pendingEmailVerificationSent
        );
    }

    private void EnsureEmailAvailable(string email, Guid currentUserId)
    {
        var alreadyExists = _context.users
            .AsNoTracking()
            .Any(x => x.id != currentUserId && (x.email == email || x.pending_email == email));

        if (alreadyExists)
            throw new ExpectedException("Já existe uma conta com esse email.", HttpStatusCode.Conflict);
    }

    private static void ClearPendingEmailChange(User user)
    {
        user.pending_email = null;
        user.pending_email_verification_secret = null;
        user.pending_email_verification_sent_at = null;
    }

    private async Task SendPendingEmailVerificationEmail(User user)
    {
        if (string.IsNullOrWhiteSpace(user.pending_email))
            throw new ExpectedException("Novo email pendente não encontrado para esta conta.", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(user.pending_email_verification_secret))
            throw new ExpectedException("Código de verificação indisponível para esta alteração de email.", HttpStatusCode.BadRequest);

        var code = TotpUtility.GenerateCode(
            user.pending_email_verification_secret,
            periodSeconds: EmailVerificationPeriodSeconds,
            digits: EmailVerificationDigits);

        var message = EmailVerificationEmailTemplate.BuildEmailChange(user, user.pending_email, code, _clientBaseUrl);

        await _email.Notify(user.pending_email, "RendaTop | Verificação de alteração de email", message, isHtml: true);
    }

    private Plan? GetActiveSubscriptionPlan(Guid userId)
    {
        var planId = _context.subscriptions
            .AsNoTracking()
            .Where(s => s.user_id == userId && s.status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.created_at)
            .Select(s => s.plan_id)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(planId))
            return null;

        return Plans.GetById(planId);
    }

    private bool CanUseWhatsAppNotifications(Guid userId) =>
        GetActiveSubscriptionPlan(userId)?.whatsapp_notifications == true;

    private bool CanUseCalendarIcs(Guid userId) =>
        GetActiveSubscriptionPlan(userId)?.calendar_ics == true;

    private AiDocumentExtractionAccessResponse GetAiDocumentExtractionAccess(Guid userId)
    {
        var plan = SubscriptionFeatureAccess.GetEffectivePlan(_context, userId);
        var currentUsage = SubscriptionFeatureAccess.GetAiUsageCountInMonth(
            _context,
            userId,
            SubscriptionFeatureAccess.InvestmentDocumentExtractionFeature,
            DateTime.UtcNow);

        var enabled = currentUsage < plan.ai_monthly_limit;
        var restrictionMessage = enabled
            ? null
            : $"Seu plano {plan.name} permite {plan.ai_monthly_limit} leituras de comprovantes por mês. Faça upgrade para continuar usando este recurso.";

        return new AiDocumentExtractionAccessResponse(
            enabled,
            currentUsage,
            plan.ai_monthly_limit,
            restrictionMessage);
    }

    private string? BuildPublicCalendarUrl(Guid? token)
    {
        if (token is null) return null;

        var publicApiUrl = Environment.GetEnvironmentVariable("PUBLIC_API_URL")?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(publicApiUrl))
            return $"{publicApiUrl}/public/calendar/{token}.ics";

        return $"{Request.Scheme}://{Request.Host}/public/calendar/{token}.ics";
    }

    private static string? SanitizeTelegramChatId(string? chatId)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            return null;

        return chatId.Trim();
    }

    private async Task DeleteUserAccountAsync(User user)
    {
        var userId = user.id;

        var aiUsages = await _context.ai_usages
            .Where(x => x.user_id == userId)
            .ToListAsync();

        var browserPushSubscriptions = await _context.browser_push_subscriptions
            .Where(x => x.user_id == userId)
            .ToListAsync();

        var notifications = await _context.notifications
            .Where(x => x.user_id == userId)
            .ToListAsync();

        var subscriptions = await _context.subscriptions
            .Where(x => x.user_id == userId)
            .ToListAsync();

        var subscriptionCharges = await _context.subscription_charges
            .Where(x => x.user_id == userId)
            .ToListAsync();

        var blogPosts = await _context.blog_posts
            .Include(x => x.assets)
            .Include(x => x.social_publications)
            .Where(x => x.author_user_id == userId)
            .ToListAsync();

        var supportTickets = await _context.support_tickets
            .Include(x => x.messages!)
                .ThenInclude(x => x.attachments)
            .Include(x => x.status_history)
            .Where(x => x.requester_user_id == userId)
            .ToListAsync();

        var supportMessagesFromOtherTickets = await _context.support_ticket_messages
            .Include(x => x.attachments)
            .Where(x => x.sender_user_id == userId && x.ticket.requester_user_id != userId)
            .ToListAsync();

        var supportStatusHistoryFromOtherTickets = await _context.support_ticket_status_history
            .Where(x => x.actor_user_id == userId && x.ticket.requester_user_id != userId)
            .ToListAsync();

        var recurringInvestments = await _context.recurring_investments
            .Where(x => x.owner_id == userId)
            .ToListAsync();

        var investments = await _context.investments
            .Include(x => x.redemptions)
            .Where(x => EF.Property<Guid>(x, "ownerid") == userId)
            .ToListAsync();

        var moneyBoxes = await _context.money_boxes
            .Where(x => x.owner_id == userId)
            .ToListAsync();

        var wallets = await _context.wallets
            .Where(x => x.owner_id == userId)
            .ToListAsync();

        var investmentRedemptions = investments
            .SelectMany(x => x.redemptions ?? [])
            .ToList();

        var supportTicketMessages = supportTickets
            .SelectMany(x => x.messages ?? [])
            .ToList();

        var supportTicketAttachments = supportTicketMessages
            .SelectMany(x => x.attachments ?? [])
            .Concat(supportMessagesFromOtherTickets.SelectMany(x => x.attachments ?? []))
            .ToList();

        var supportTicketStatusHistory = supportTickets
            .SelectMany(x => x.status_history ?? [])
            .ToList();

        var blogPostAssets = blogPosts
            .SelectMany(x => x.assets ?? [])
            .ToList();

        var blogPostSocialPublications = blogPosts
            .SelectMany(x => x.social_publications ?? [])
            .ToList();

        _context.support_ticket_message_attachments.RemoveRange(supportTicketAttachments);
        _context.support_ticket_messages.RemoveRange(supportMessagesFromOtherTickets);
        _context.support_ticket_status_history.RemoveRange(supportStatusHistoryFromOtherTickets);
        _context.support_ticket_messages.RemoveRange(supportTicketMessages);
        _context.support_ticket_status_history.RemoveRange(supportTicketStatusHistory);
        _context.support_tickets.RemoveRange(supportTickets);

        _context.blog_post_social_publications.RemoveRange(blogPostSocialPublications);
        _context.blog_post_assets.RemoveRange(blogPostAssets);
        _context.blog_posts.RemoveRange(blogPosts);

        _context.redemptions.RemoveRange(investmentRedemptions);
        _context.investments.RemoveRange(investments);
        _context.recurring_investments.RemoveRange(recurringInvestments);
        _context.money_boxes.RemoveRange(moneyBoxes);
        _context.wallets.RemoveRange(wallets);

        _context.subscription_charges.RemoveRange(subscriptionCharges);
        _context.subscriptions.RemoveRange(subscriptions);
        _context.notifications.RemoveRange(notifications);
        _context.browser_push_subscriptions.RemoveRange(browserPushSubscriptions);
        _context.ai_usages.RemoveRange(aiUsages);

        _context.users.Remove(user);

        await _context.SaveChangesAsync();
    }
}

public record UserSettingsRequest(
    string name,
    string email,
    string? password,
    string? phone,
    bool notify_whatsapp,
    bool notify_telegram,
    string? telegram_chat_id,
    bool notify_email,
    bool calendar_public_enabled,
    bool? totp_enabled = null
);

public record NotificationTestRequest(
    string? phone,
    string? telegram_chat_id
);

public record PendingEmailVerificationRequest(
    string code
);

public record UserSettingsResponse(
    string name,
    string email,
    string phone,
    string cpf,
    bool notify_whatsapp,
    bool notify_telegram,
    string? telegram_chat_id,
    bool notify_email,
    bool notify_browser,
    bool calendar_public_enabled,
    string? calendar_public_url,
    bool totp_enabled,
    bool whatsapp_notifications_enabled,
    bool calendar_ics_enabled,
    bool ai_document_extraction_enabled,
    int ai_document_extraction_current_usage,
    int ai_document_extraction_monthly_limit,
    string? ai_document_extraction_restriction_message,
    UserType user_type,
    string? pending_email,
    bool? pending_email_verification_sent = null
);

public record AiDocumentExtractionAccessResponse(
    bool enabled,
    int current_usage,
    int monthly_limit,
    string? restriction_message
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

public record BrowserPushSubscriptionRequest(
    string endpoint,
    string p256dh,
    string auth,
    string? user_agent
);

public record BrowserPushUnsubscribeRequest(
    string endpoint
);

public record BrowserPushPublicKeyResponse(
    bool enabled,
    string? public_key
);

public record DeleteOwnAccountRequest(
    bool confirm_first_step,
    bool confirm_second_step,
    string? confirmation_text
);
