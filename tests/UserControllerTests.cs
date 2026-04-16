using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Controllers;
using server.Domain;
using server.Utils;

namespace tests;

public class UserControllerTests
{
    [Fact]
    public async Task UpdateSettings_UpdatesUserNameAndReturnsUpdatedResponse()
    {
        using var fixture = new UserControllerFixture();

        var result = await fixture.Controller.UpdateSettings(new UserSettingsRequest(
            "Nome Atualizado",
            "updated@example.com",
            null,
            "65999999999",
            false,
            false,
            null,
            true,
            false
        ));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSettingsResponse>(okResult.Value);

        Assert.Equal("Nome Atualizado", response.name);
        Assert.Equal("test@example.com", response.email);
        Assert.Equal("updated@example.com", response.pending_email);
        Assert.True(response.pending_email_verification_sent);
        Assert.Equal(UserType.Common, response.user_type);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal("Nome Atualizado", savedUser.name);
        Assert.Equal("test@example.com", savedUser.email);
        Assert.Equal("updated@example.com", savedUser.pending_email);
        Assert.False(string.IsNullOrWhiteSpace(savedUser.pending_email_verification_secret));
        Assert.Single(fixture.Email.Messages);
        Assert.Equal("updated@example.com", fixture.Email.Messages[0].ToEmail);
    }

    [Fact]
    public async Task UpdateSettings_ThrowsWhenNameIsBlank()
    {
        using var fixture = new UserControllerFixture();

        var exception = await Assert.ThrowsAsync<ExpectedException>(() => fixture.Controller.UpdateSettings(new UserSettingsRequest(
            "   ",
            "test@example.com",
            null,
            "",
            false,
            false,
            null,
            true,
            false
        )));

        Assert.Equal("Nome é obrigatório.", exception.Message);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal("Test User", savedUser.name);
    }

    [Fact]
    public async Task UpdateSettings_ThrowsWhenTelegramEnabledWithoutChatId()
    {
        using var fixture = new UserControllerFixture();

        var exception = await Assert.ThrowsAsync<ExpectedException>(() => fixture.Controller.UpdateSettings(new UserSettingsRequest(
            "Test User",
            "test@example.com",
            null,
            "",
            false,
            true,
            null,
            true,
            false
        )));

        Assert.Equal("Informe o Chat ID do Telegram para habilitar notificações por Telegram.", exception.Message);
    }

    [Fact]
    public async Task TestTelegram_UsesRequestChatIdWithoutPersistingIt()
    {
        using var fixture = new UserControllerFixture();

        var result = await fixture.Controller.TestTelegram(new NotificationTestRequest(null, "123456789"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GenericMessageResponse>(okResult.Value);
        Assert.Equal("123456789", fixture.Notification.LastChatId);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Null(savedUser.telegram_chat_id);
    }

    [Fact]
    public void GetBrowserPushPublicKey_ReturnsConfiguredPublicKey()
    {
        using var fixture = new UserControllerFixture();

        var result = fixture.Controller.GetBrowserPushPublicKey();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BrowserPushPublicKeyResponse>(okResult.Value);

        Assert.True(response.enabled);
        Assert.Equal("public-key", response.public_key);
    }

    [Fact]
    public void SubscribeBrowserPush_PersistsSubscriptionAndEnablesBrowserNotifications()
    {
        using var fixture = new UserControllerFixture();

        var result = fixture.Controller.SubscribeBrowserPush(new BrowserPushSubscriptionRequest(
            "https://push.example.test/subscriptions/1",
            "p256dh-key",
            "auth-key",
            "Test Browser"
        ));

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GenericMessageResponse>(okResult.Value);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        var subscription = assertionContext.browser_push_subscriptions.Single(x => x.user_id == fixture.User.id);

        Assert.True(savedUser.notify_browser);
        Assert.Equal("https://push.example.test/subscriptions/1", subscription.endpoint);
        Assert.Equal("p256dh-key", subscription.p256dh);
        Assert.Equal("auth-key", subscription.auth);
    }

    [Fact]
    public async Task UpdateSettings_WithPendingEmailAndOriginalEmail_CancelsPendingChange()
    {
        using var fixture = new UserControllerFixture();
        fixture.User.pending_email = "updated@example.com";
        fixture.User.pending_email_verification_secret = TotpUtility.GenerateBase32Secret();
        fixture.User.pending_email_verification_sent_at = DateTime.UtcNow;
        fixture.Context.SaveChanges();

        var result = await fixture.Controller.UpdateSettings(new UserSettingsRequest(
            "Test User",
            "test@example.com",
            null,
            "",
            false,
            false,
            null,
            true,
            false
        ));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSettingsResponse>(okResult.Value);

        Assert.Null(response.pending_email);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Null(savedUser.pending_email);
        Assert.Null(savedUser.pending_email_verification_secret);
        Assert.Null(savedUser.pending_email_verification_sent_at);
    }

    [Fact]
    public void VerifyPendingEmail_UpdatesConfirmedEmail()
    {
        using var fixture = new UserControllerFixture();
        var secret = TotpUtility.GenerateBase32Secret();
        fixture.User.pending_email = "updated@example.com";
        fixture.User.pending_email_verification_secret = secret;
        fixture.User.pending_email_verification_sent_at = DateTime.UtcNow;
        fixture.Context.SaveChanges();

        var code = TotpUtility.GenerateCode(secret, periodSeconds: 300, digits: 6);
        var result = fixture.Controller.VerifyPendingEmail(new PendingEmailVerificationRequest(code));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSettingsResponse>(okResult.Value);

        Assert.Equal("updated@example.com", response.email);
        Assert.Null(response.pending_email);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal("updated@example.com", savedUser.email);
        Assert.Null(savedUser.pending_email);
        Assert.Null(savedUser.pending_email_verification_secret);
        Assert.Null(savedUser.pending_email_verification_sent_at);
    }

    [Fact]
    public async Task ResendPendingEmailVerification_RotatesSecretAndSendsEmail()
    {
        using var fixture = new UserControllerFixture();
        fixture.User.pending_email = "updated@example.com";
        fixture.User.pending_email_verification_secret = TotpUtility.GenerateBase32Secret();
        fixture.User.pending_email_verification_sent_at = DateTime.UtcNow.AddMinutes(-2);
        fixture.Context.SaveChanges();
        var originalSecret = fixture.User.pending_email_verification_secret;

        var result = await fixture.Controller.ResendPendingEmailVerification();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GenericMessageResponse>(okResult.Value);
        Assert.Equal("Novo código de verificação enviado para seu novo email.", response.message);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal("updated@example.com", savedUser.pending_email);
        Assert.NotEqual(originalSecret, savedUser.pending_email_verification_secret);
        Assert.Single(fixture.Email.Messages);
        Assert.Equal("updated@example.com", fixture.Email.Messages[0].ToEmail);
    }

    [Fact]
    public void CancelPendingEmailVerification_ClearsPendingChange()
    {
        using var fixture = new UserControllerFixture();
        fixture.User.pending_email = "updated@example.com";
        fixture.User.pending_email_verification_secret = TotpUtility.GenerateBase32Secret();
        fixture.User.pending_email_verification_sent_at = DateTime.UtcNow;
        fixture.Context.SaveChanges();

        var result = fixture.Controller.CancelPendingEmailVerification();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSettingsResponse>(okResult.Value);

        Assert.Equal("test@example.com", response.email);
        Assert.Null(response.pending_email);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal("test@example.com", savedUser.email);
        Assert.Null(savedUser.pending_email);
        Assert.Null(savedUser.pending_email_verification_secret);
        Assert.Null(savedUser.pending_email_verification_sent_at);
    }

    [Fact]
    public async Task UpdateSettings_WithSsoUser_StillRequiresEmailVerification()
    {
        using var fixture = new UserControllerFixture();
        fixture.User.auth_provider = AuthProvider.Google;
        fixture.Context.SaveChanges();

        var result = await fixture.Controller.UpdateSettings(new UserSettingsRequest(
            "Test User",
            "google-updated@example.com",
            null,
            "",
            false,
            false,
            null,
            true,
            false
        ));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSettingsResponse>(okResult.Value);

        Assert.Equal("test@example.com", response.email);
        Assert.Equal("google-updated@example.com", response.pending_email);
        Assert.True(response.pending_email_verification_sent);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal(AuthProvider.Google, savedUser.auth_provider);
        Assert.Equal("test@example.com", savedUser.email);
        Assert.Equal("google-updated@example.com", savedUser.pending_email);
        Assert.False(string.IsNullOrWhiteSpace(savedUser.pending_email_verification_secret));
    }

    private sealed class UserControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public UserController Controller { get; }
        public User User { get; }
        public FakeNotification Notification { get; }
        public RecordingEmailNotification Email { get; }

        public UserControllerFixture()
        {
            var databaseName = Guid.NewGuid().ToString("N");
            _options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            Context = new Context(_options);
            User = new User
            {
                name = "Test User",
                email = "test@example.com",
                password = "secret",
                notify_email = true
            };

            Context.users.Add(User);
            Context.SaveChanges();

            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = User;
            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

            Notification = new FakeNotification();
            Email = new RecordingEmailNotification();

            Controller = new UserController(
                httpContextAccessor,
                new TestContextFactory(_options),
                Notification,
                new FakeWhatsAppNotification(),
                Email,
                new FakeBrowserPushNotification());
        }

        public Context CreateAssertionContext() => new(_options);

        public void Dispose()
        {
            Context.Dispose();
        }
    }

    private sealed class TestContextFactory : IDbContextFactory<Context>
    {
        private readonly DbContextOptions<Context> _options;

        public TestContextFactory(DbContextOptions<Context> options)
        {
            _options = options;
        }

        public Context CreateDbContext()
        {
            return new Context(_options);
        }
    }

    private sealed class FakeNotification : INotification
    {
        public string? LastChatId { get; private set; }

        public Task Notify(string title, string message, string? chatId = null)
        {
            LastChatId = chatId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWhatsAppNotification : IWhatsAppNotification
    {
        public Task Notify(string phone, string title, string message) => Task.CompletedTask;
    }

    private sealed class RecordingEmailNotification : IEmailNotification
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task Notify(string destination, string title, string message, bool isHtml = false)
        {
            Messages.Add(new EmailMessage(destination, title, message));
            return Task.CompletedTask;
        }
    }

    private sealed record EmailMessage(string ToEmail, string Title, string Message);

    private sealed class FakeBrowserPushNotification : IBrowserPushNotification
    {
        public bool IsConfigured => true;
        public string PublicKey => "public-key";

        public Task SendAsync(BrowserPushSubscription subscription, BrowserPushMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
