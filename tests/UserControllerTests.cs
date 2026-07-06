using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Controllers;
using server.Domain;
using server.RequestObjects;
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

    [Fact]
    public async Task DeleteOwnAccount_ThrowsWhenConfirmationIsIncomplete()
    {
        using var fixture = new UserControllerFixture();

        var exception = await Assert.ThrowsAsync<ExpectedException>(() => fixture.Controller.DeleteOwnAccount(
            new DeleteOwnAccountRequest(
                confirm_first_step: true,
                confirm_second_step: false,
                confirmation_text: "EXCLUIR"
            )));

        Assert.Equal("Confirmação de exclusão incompleta.", exception.Message);

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Single(assertionContext.users);
    }

    [Fact]
    public async Task DeleteOwnAccount_RemovesUserAndOwnedData()
    {
        using var fixture = new UserControllerFixture();
        var bank = new Bank
        {
            Code = 1,
            Name = "Banco Teste",
            CompanyName = "Banco Teste S.A.",
            Cnpj = "00000000000191",
            Color = "#000000"
        };
        fixture.Context.banks.Add(bank);

        var wallet = new Wallet
        {
            owner_id = fixture.User.id,
            name = "Carteira Teste"
        };
        fixture.Context.wallets.Add(wallet);

        var moneyBox = new MoneyBox
        {
            owner_id = fixture.User.id,
            name = "Cofrinho Teste"
        };
        fixture.Context.money_boxes.Add(moneyBox);

        var investment = new Investment(new InvestmentRequest
        {
            title = "CDB Teste",
            bank_code = 1,
            wallet_id = wallet.id,
            money_box_id = moneyBox.id,
            date_buy = DateTime.UtcNow.AddDays(-10),
            date_expected_sell = DateTime.UtcNow.AddDays(30),
            value = 1000m,
            index = IdexesType.CDI,
            index_percent = 100m,
            index_value = 0m,
            taxes = true
        }, fixture.User, bank);
        fixture.Context.investments.Add(investment);

        var redemption = new Redemption(investment, new RedemptionRequest
        {
            title = "Resgate parcial",
            date = DateTime.UtcNow,
            value = 100m
        });
        fixture.Context.redemptions.Add(redemption);

        var recurringInvestment = new RecurringInvestment(new RecurringInvestmentRequest
        {
            title = "Aporte mensal",
            wallet_id = wallet.id,
            bank_code = 1,
            value = 200m,
            index = IdexesType.CDI,
            index_percent = 100m,
            index_value = 0m,
            taxes = true,
            liquidity_daily = false,
            duration_days = 365,
            frequency = RecurringInvestmentFrequency.Monthly,
            day_of_month = 10,
            months = new List<int> { 1, 2, 3 },
            active = true
        }, fixture.User, bank);
        fixture.Context.recurring_investments.Add(recurringInvestment);

        var subscription = new Subscription
        {
            user_id = fixture.User.id,
            plan_id = "plus",
            status = SubscriptionStatus.Active,
            payment_method = "credit_card",
            current_period_start = DateTime.UtcNow.AddDays(-10),
            current_period_end = DateTime.UtcNow.AddDays(20),
        };
        fixture.Context.subscriptions.Add(subscription);

        var charge = new SubscriptionCharge
        {
            subscription = subscription,
            subscription_id = subscription.id,
            user = fixture.User,
            user_id = fixture.User.id,
            plan_id = "plus",
            payment_method = "credit_card",
            amount = 19.90m,
            payer_cpf = "12345678901",
            status = SubscriptionChargeStatus.Pending,
            charge_kind = SubscriptionChargeKind.Renewal,
            billing_period_start = DateTime.UtcNow,
            billing_period_end = DateTime.UtcNow.AddMonths(1),
            due_at = DateTime.UtcNow.AddDays(7)
        };
        fixture.Context.subscription_charges.Add(charge);

        fixture.Context.browser_push_subscriptions.Add(new BrowserPushSubscription
        {
            user_id = fixture.User.id,
            endpoint = "https://push.example.test/subscriptions/delete-me",
            p256dh = "p256dh",
            auth = "auth"
        });

        fixture.Context.notifications.Add(new Notification
        {
            user_id = fixture.User.id,
            user = fixture.User,
            title = "Notificação",
            message = "Mensagem"
        });

        fixture.Context.ai_usages.Add(new AiUsage
        {
            user_id = fixture.User.id,
            user = fixture.User,
            feature = "investment_document_extraction",
            provider = "openai"
        });

        fixture.Context.SaveChanges();

        var result = await fixture.Controller.DeleteOwnAccount(new DeleteOwnAccountRequest(
            confirm_first_step: true,
            confirm_second_step: true,
            confirmation_text: "EXCLUIR"
        ));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GenericMessageResponse>(okResult.Value);
        Assert.Equal("Sua conta foi excluída permanentemente.", response.message);

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Empty(assertionContext.users);
        Assert.Empty(assertionContext.wallets);
        Assert.Empty(assertionContext.money_boxes);
        Assert.Empty(assertionContext.investments);
        Assert.Empty(assertionContext.redemptions);
        Assert.Empty(assertionContext.recurring_investments);
        Assert.Empty(assertionContext.subscriptions);
        Assert.Empty(assertionContext.subscription_charges);
        Assert.Empty(assertionContext.browser_push_subscriptions);
        Assert.Empty(assertionContext.notifications);
        Assert.Empty(assertionContext.ai_usages);
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
