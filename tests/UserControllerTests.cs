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
    public void UpdateSettings_UpdatesUserNameAndReturnsUpdatedResponse()
    {
        using var fixture = new UserControllerFixture();

        var result = fixture.Controller.UpdateSettings(new UserSettingsRequest(
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
        Assert.Equal("updated@example.com", response.email);
        Assert.Equal(UserType.Common, response.user_type);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal("Nome Atualizado", savedUser.name);
        Assert.Equal("updated@example.com", savedUser.email);
    }

    [Fact]
    public void UpdateSettings_ThrowsWhenNameIsBlank()
    {
        using var fixture = new UserControllerFixture();

        var exception = Assert.Throws<ExpectedException>(() => fixture.Controller.UpdateSettings(new UserSettingsRequest(
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
    public void UpdateSettings_ThrowsWhenTelegramEnabledWithoutChatId()
    {
        using var fixture = new UserControllerFixture();

        var exception = Assert.Throws<ExpectedException>(() => fixture.Controller.UpdateSettings(new UserSettingsRequest(
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

    private sealed class UserControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public UserController Controller { get; }
        public User User { get; }
        public FakeNotification Notification { get; }

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

            Controller = new UserController(
                httpContextAccessor,
                new TestContextFactory(_options),
                Notification,
                new FakeWhatsAppNotification(),
                new FakeEmailNotification(),
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

    private sealed class FakeEmailNotification : IEmailNotification
    {
        public Task Notify(string destination, string title, string message, bool isHtml = false) => Task.CompletedTask;
    }

    private sealed class FakeBrowserPushNotification : IBrowserPushNotification
    {
        public bool IsConfigured => true;
        public string PublicKey => "public-key";

        public Task SendAsync(BrowserPushSubscription subscription, BrowserPushMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
