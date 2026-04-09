using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using server.Controllers;
using server.Domain;
using server.Payments;
using server.Services;
using server.Utils;

namespace tests;

public class SubscriptionBillingServiceTests
{
    [Fact]
    public async Task CreateInitialCardSubscriptionAsync_SendsReceiptAndCreatesApprovedCharge()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.PaymentProvider.CardResult = new PaymentResult
        {
            payment_id = "card-approved-1",
            status = "approved",
            status_detail = "accredited",
            amount = 6.9m,
            approved_at = DateTime.UtcNow
        };
        fixture.PaymentProvider.SavedCardResult = ("cust-1", "saved-card-1");

        await fixture.Service.SavePayerCpfAsync(fixture.User.id, "12345678909");
        var result = await fixture.Service.CreateInitialCardSubscriptionAsync(
            fixture.User.id,
            Plans.GetById("plus")!,
            "card-token",
            "visa",
            "credit_card",
            "",
            1);

        Assert.Equal("approved", result.status);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var charge = assertionContext.subscription_charges.Single();

        Assert.Equal(SubscriptionStatus.Active, subscription.status);
        Assert.Equal("credit_card", subscription.payment_method);
        Assert.Equal("card-approved-1", subscription.mp_payment_id);
        Assert.Equal("cust-1", subscription.mp_customer_id);
        Assert.Equal("saved-card-1", subscription.mp_card_id);

        Assert.Equal(SubscriptionChargeStatus.Approved, charge.status);
        Assert.Equal(SubscriptionChargeKind.Initial, charge.charge_kind);
        Assert.Equal("12345678909", charge.payer_cpf);
        Assert.NotNull(charge.receipt_sent_at);

        var email = Assert.Single(fixture.Email.Messages);
        Assert.True(email.IsHtml);
        Assert.Contains("<html", email.Message);
        Assert.Contains("icon.png", email.Message);
        Assert.Contains("12345678909", email.Message);
        Assert.Contains("nova cobrança automática", email.Message);
    }

    [Fact]
    public async Task CreateInitialPixSubscriptionAsync_DoesNotSendReceiptWhilePending()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.PaymentProvider.PixResult = new PaymentResult
        {
            payment_id = "pix-pending-1",
            status = "pending",
            status_detail = "pending_waiting_transfer",
            amount = 6.9m,
            pix_qr_code = "pix-code",
            pix_qr_code_base64 = "base64"
        };

        await fixture.Service.SavePayerCpfAsync(fixture.User.id, "12345678909");
        var result = await fixture.Service.CreateInitialPixSubscriptionAsync(
            fixture.User.id,
            Plans.GetById("plus")!,
            "Hudson",
            "Ventura");

        Assert.Equal("pending", result.status);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var charge = assertionContext.subscription_charges.Single();

        Assert.Equal(SubscriptionStatus.PendingPayment, subscription.status);
        Assert.Equal(SubscriptionChargeStatus.Pending, charge.status);
        Assert.Equal("pix-code", charge.pix_qr_code);
        Assert.Null(charge.receipt_sent_at);
        Assert.Empty(fixture.Email.Messages);
    }

    [Fact]
    public async Task RefreshPaymentStatusAsync_ApprovesPendingPixAndSendsReceipt()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.PaymentProvider.PixResult = new PaymentResult
        {
            payment_id = "pix-pending-2",
            status = "pending",
            amount = 6.9m,
            pix_qr_code = "pix-code"
        };

        await fixture.Service.SavePayerCpfAsync(fixture.User.id, "12345678909");
        await fixture.Service.CreateInitialPixSubscriptionAsync(
            fixture.User.id,
            Plans.GetById("plus")!,
            "Hudson",
            "Ventura");

        fixture.PaymentProvider.PaymentStatusResults["pix-pending-2"] = new PaymentResult
        {
            payment_id = "pix-pending-2",
            status = "approved",
            status_detail = "accredited",
            amount = 6.9m,
            approved_at = DateTime.UtcNow
        };

        var result = await fixture.Service.RefreshPaymentStatusAsync(fixture.User.id, "pix-pending-2");

        Assert.Equal("approved", result.status);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var charge = assertionContext.subscription_charges.Single();

        Assert.Equal(SubscriptionStatus.Active, subscription.status);
        Assert.Equal(SubscriptionChargeStatus.Approved, charge.status);
        Assert.NotNull(charge.receipt_sent_at);
        Assert.Single(fixture.Email.Messages);
    }

    [Fact]
    public async Task CreateInitialPixSubscriptionAsync_DoesNotCancelExistingActiveSubscriptionWhilePending()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddDays(10), currentCycleReminderSentAt: null);
        fixture.PaymentProvider.PixResult = new PaymentResult
        {
            payment_id = "pix-pending-keep-active",
            status = "pending",
            amount = 6.9m,
            pix_qr_code = "pix-code"
        };

        await fixture.Service.SavePayerCpfAsync(fixture.User.id, "12345678909");
        await fixture.Service.CreateInitialPixSubscriptionAsync(
            fixture.User.id,
            Plans.GetById("pro")!,
            "Hudson",
            "Ventura");

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Equal(2, assertionContext.subscriptions.Count());
        Assert.Equal(SubscriptionStatus.Active, assertionContext.subscriptions.First(x => x.plan_id == "plus").status);
        Assert.Equal(SubscriptionStatus.PendingPayment, assertionContext.subscriptions.First(x => x.plan_id == "pro").status);
    }

    [Fact]
    public async Task ProcessDueTomorrowRenewalNotificationsAsync_CreatesPendingPixRenewalAndReminder()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("pix", DateTime.UtcNow.Date.AddDays(1), currentCycleReminderSentAt: null);
        fixture.PaymentProvider.PixResult = new PaymentResult
        {
            payment_id = "renew-pix-1",
            status = "pending",
            amount = 6.9m,
            pix_qr_code = "renew-pix-code",
            pix_qr_code_base64 = "renew-pix-base64",
            date_of_expiration = DateTime.UtcNow.Date.AddDays(1)
        };

        await fixture.Service.ProcessDueTomorrowRenewalNotificationsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var charges = assertionContext.subscription_charges
            .OrderBy(x => x.created_at)
            .ToList();

        Assert.Equal(2, charges.Count);
        var renewalCharge = Assert.Single(charges, x => x.charge_kind == SubscriptionChargeKind.Renewal);
        Assert.Equal(SubscriptionChargeStatus.Pending, renewalCharge.status);
        Assert.Equal("renew-pix-code", renewalCharge.pix_qr_code);
        Assert.NotNull(renewalCharge.reminder_sent_at);

        var email = Assert.Single(fixture.Email.Messages);
        Assert.Contains("deverá concluir o novo pagamento", email.Message);
    }

    [Fact]
    public async Task ProcessDueTomorrowRenewalNotificationsAsync_SendsCardReminderOnlyOnce()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.Date.AddDays(1), currentCycleReminderSentAt: null);

        await fixture.Service.ProcessDueTomorrowRenewalNotificationsAsync();
        await fixture.Service.ProcessDueTomorrowRenewalNotificationsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var currentCharge = assertionContext.subscription_charges.Single();

        Assert.NotNull(currentCharge.reminder_sent_at);
        Assert.Single(fixture.Email.Messages);
    }

    [Fact]
    public async Task ProcessDueCardRenewalsAsync_RenewsSubscriptionAndSendsReceipt()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddMinutes(-5), currentCycleReminderSentAt: DateTime.UtcNow.AddHours(-1));
        fixture.PaymentProvider.SavedCardPaymentResult = new PaymentResult
        {
            payment_id = "renew-card-1",
            status = "approved",
            status_detail = "accredited",
            amount = 6.9m,
            approved_at = DateTime.UtcNow
        };

        await fixture.Service.ProcessDueCardRenewalsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var renewalCharge = assertionContext.subscription_charges
            .OrderByDescending(x => x.created_at)
            .First(x => x.charge_kind == SubscriptionChargeKind.Renewal);

        Assert.Equal(SubscriptionStatus.Active, subscription.status);
        Assert.Equal(SubscriptionChargeStatus.Approved, renewalCharge.status);
        Assert.NotNull(renewalCharge.receipt_sent_at);
        Assert.Single(fixture.Email.Messages);
    }

    [Fact]
    public async Task ProcessDueCardRenewalsAsync_ExpiresSubscription_WhenProviderRejectsForInsufficientAmount()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddMinutes(-5), currentCycleReminderSentAt: DateTime.UtcNow.AddHours(-1));
        fixture.PaymentProvider.SavedCardPaymentResult = new PaymentResult
        {
            payment_id = "renew-card-rejected-insufficient",
            status = "rejected",
            status_detail = "cc_rejected_insufficient_amount",
            amount = 6.9m
        };

        await fixture.Service.ProcessDueCardRenewalsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var renewalCharge = assertionContext.subscription_charges
            .OrderByDescending(x => x.created_at)
            .First(x => x.charge_kind == SubscriptionChargeKind.Renewal);

        Assert.Equal(SubscriptionStatus.Expired, subscription.status);
        Assert.Equal(SubscriptionChargeStatus.Rejected, renewalCharge.status);
        Assert.Equal("renew-card-rejected-insufficient", renewalCharge.provider_payment_id);
        Assert.Equal("cc_rejected_insufficient_amount", renewalCharge.provider_status_detail);
        Assert.Null(renewalCharge.receipt_sent_at);
        Assert.Empty(fixture.Email.Messages);
    }

    [Fact]
    public async Task ProcessDueCardRenewalsAsync_ExpiresSubscription_WhenProviderRejectsForSecurityCode()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddMinutes(-5), currentCycleReminderSentAt: DateTime.UtcNow.AddHours(-1));
        fixture.PaymentProvider.SavedCardPaymentResult = new PaymentResult
        {
            payment_id = "renew-card-rejected-cvv",
            status = "rejected",
            status_detail = "cc_rejected_bad_filled_security_code",
            amount = 6.9m
        };

        await fixture.Service.ProcessDueCardRenewalsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var renewalCharge = assertionContext.subscription_charges
            .OrderByDescending(x => x.created_at)
            .First(x => x.charge_kind == SubscriptionChargeKind.Renewal);

        Assert.Equal(SubscriptionStatus.Expired, subscription.status);
        Assert.Equal(SubscriptionChargeStatus.Rejected, renewalCharge.status);
        Assert.Equal("renew-card-rejected-cvv", renewalCharge.provider_payment_id);
        Assert.Equal("cc_rejected_bad_filled_security_code", renewalCharge.provider_status_detail);
        Assert.Null(renewalCharge.receipt_sent_at);
        Assert.Empty(fixture.Email.Messages);
    }

    [Fact]
    public async Task ExpireUnpaidRenewalsAsync_ExpiresSubscriptionAndPendingRenewalCharge()
    {
        using var fixture = new SubscriptionBillingFixture();
        var expiredAt = DateTime.UtcNow.AddMinutes(-10);
        fixture.SeedActiveSubscriptionWithCharge("pix", expiredAt, currentCycleReminderSentAt: DateTime.UtcNow.AddDays(-1));
        fixture.SeedPendingRenewalCharge(expiredAt);

        await fixture.Service.ExpireUnpaidRenewalsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var renewalCharge = assertionContext.subscription_charges
            .OrderByDescending(x => x.created_at)
            .First(x => x.charge_kind == SubscriptionChargeKind.Renewal);

        Assert.Equal(SubscriptionStatus.Expired, subscription.status);
        Assert.Equal(SubscriptionChargeStatus.Expired, renewalCharge.status);
    }

    [Fact]
    public async Task CancelActiveSubscriptionAsync_SchedulesPixCancellationAtPeriodEnd_AndCancelsFutureRenewal()
    {
        using var fixture = new SubscriptionBillingFixture();
        var periodEnd = DateTime.UtcNow.AddDays(7);
        fixture.SeedActiveSubscriptionWithCharge("pix", periodEnd, currentCycleReminderSentAt: null);
        fixture.SeedPendingRenewalCharge(periodEnd);

        var result = await fixture.Service.CancelActiveSubscriptionAsync(
            fixture.User.id,
            confirmed: true,
            SubscriptionCancellationMode.EndOfPeriod);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();
        var renewalCharge = assertionContext.subscription_charges.Single(x => x.charge_kind == SubscriptionChargeKind.Renewal);

        Assert.True(result.cancelled);
        Assert.True(result.scheduled);
        Assert.Equal(SubscriptionStatus.Active, subscription.status);
        Assert.True(subscription.cancel_at_period_end);
        Assert.NotNull(subscription.cancellation_requested_at);
        Assert.Equal(SubscriptionChargeStatus.Cancelled, renewalCharge.status);
        var email = Assert.Single(fixture.Email.Messages);
        Assert.True(email.IsHtml);
        Assert.Contains("Cancelamento de assinatura", email.Message);
        Assert.Contains("Sem estorno", email.Message);
        Assert.Contains("ate o fim do periodo atual", email.Message);
    }

    [Fact]
    public async Task CancelActiveSubscriptionAsync_SchedulesCardCancellationAtPeriodEnd_WhenUserKeepsAccess()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddDays(10), currentCycleReminderSentAt: null);

        var result = await fixture.Service.CancelActiveSubscriptionAsync(
            fixture.User.id,
            confirmed: true,
            SubscriptionCancellationMode.EndOfPeriod);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();

        Assert.True(result.cancelled);
        Assert.True(result.scheduled);
        Assert.Equal(SubscriptionStatus.Active, subscription.status);
        Assert.True(subscription.cancel_at_period_end);
        Assert.Empty(fixture.PaymentProvider.RefundRequests);
        var email = Assert.Single(fixture.Email.Messages);
        Assert.True(email.IsHtml);
        Assert.Contains("Cancelamento programado", email.Message);
        Assert.Contains("Sem estorno", email.Message);
    }

    [Fact]
    public async Task CancelActiveSubscriptionAsync_RefundsProratedAndCancelsImmediately_ForCard()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddDays(15), currentCycleReminderSentAt: null);

        var result = await fixture.Service.CancelActiveSubscriptionAsync(
            fixture.User.id,
            confirmed: true,
            SubscriptionCancellationMode.RefundProrated);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();

        Assert.True(result.cancelled);
        Assert.False(result.scheduled);
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.status);
        Assert.False(subscription.cancel_at_period_end);

        var refund = Assert.Single(fixture.PaymentProvider.RefundRequests);
        Assert.Equal("seed-payment", refund.PaymentId);
        Assert.True(refund.Amount > 0);
        Assert.Equal(refund.Amount, result.refunded_amount);
        var email = Assert.Single(fixture.Email.Messages);
        Assert.True(email.IsHtml);
        Assert.Contains("Assinatura cancelada", email.Message);
        Assert.Contains($"R$ {refund.Amount:N2}", email.Message);
        Assert.Contains("estorno proporcional", email.Message);
    }

    [Fact]
    public async Task CancelActiveSubscriptionAsync_RefundsProratedAndCancelsImmediately_ForPix()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("pix", DateTime.UtcNow.AddDays(15), currentCycleReminderSentAt: null);

        var result = await fixture.Service.CancelActiveSubscriptionAsync(
            fixture.User.id,
            confirmed: true,
            SubscriptionCancellationMode.RefundProrated);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();

        Assert.True(result.cancelled);
        Assert.False(result.scheduled);
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.status);
        Assert.False(subscription.cancel_at_period_end);

        var refund = Assert.Single(fixture.PaymentProvider.RefundRequests);
        Assert.Equal("seed-payment", refund.PaymentId);
        Assert.True(refund.Amount > 0);
        Assert.Equal(refund.Amount, result.refunded_amount);
        var email = Assert.Single(fixture.Email.Messages);
        Assert.True(email.IsHtml);
        Assert.Contains($"R$ {refund.Amount:N2}", email.Message);
        Assert.Contains("estorno proporcional", email.Message);
    }

    [Fact]
    public async Task ProcessDueTomorrowRenewalNotificationsAsync_DoesNotNotifyWhenCancellationWasScheduled()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.Date.AddDays(1), currentCycleReminderSentAt: null);
        fixture.UpdateSubscription(subscription =>
        {
            subscription.cancel_at_period_end = true;
            subscription.cancellation_requested_at = DateTime.UtcNow;
        });

        await fixture.Service.ProcessDueTomorrowRenewalNotificationsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var charge = assertionContext.subscription_charges.Single();

        Assert.Null(charge.reminder_sent_at);
        Assert.Empty(fixture.Email.Messages);
    }

    [Fact]
    public async Task ProcessScheduledCancellationsAsync_CancelsSubscriptionAtPeriodEnd()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("pix", DateTime.UtcNow.AddMinutes(-5), currentCycleReminderSentAt: null);
        fixture.UpdateSubscription(subscription =>
        {
            subscription.cancel_at_period_end = true;
            subscription.cancellation_requested_at = DateTime.UtcNow.AddDays(-2);
        });

        await fixture.Service.ProcessScheduledCancellationsAsync();

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();

        Assert.Equal(SubscriptionStatus.Cancelled, subscription.status);
        Assert.False(subscription.cancel_at_period_end);
    }

    [Fact]
    public async Task RevertScheduledCancellationAsync_RemovesScheduledCancellation()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddDays(10), currentCycleReminderSentAt: null);
        fixture.UpdateSubscription(subscription =>
        {
            subscription.cancel_at_period_end = true;
            subscription.cancellation_requested_at = DateTime.UtcNow;
        });

        var result = await fixture.Service.RevertScheduledCancellationAsync(
            fixture.User.id,
            confirmed: true);

        using var assertionContext = fixture.CreateAssertionContext();
        var subscription = assertionContext.subscriptions.Single();

        Assert.False(result.cancelled);
        Assert.False(result.scheduled);
        Assert.False(subscription.cancel_at_period_end);
        Assert.Null(subscription.cancellation_requested_at);
    }

    [Fact]
    public async Task RevertScheduledCancellationAsync_Throws_WhenNoScheduledCancellationExists()
    {
        using var fixture = new SubscriptionBillingFixture();
        fixture.SeedActiveSubscriptionWithCharge("credit_card", DateTime.UtcNow.AddDays(10), currentCycleReminderSentAt: null);

        var exception = await Assert.ThrowsAsync<ExpectedException>(() =>
            fixture.Service.RevertScheduledCancellationAsync(
                fixture.User.id,
                confirmed: true));

        Assert.Equal("Não existe uma programação de cancelamento para reverter.", exception.Message);
    }

    private sealed class SubscriptionBillingFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public User User { get; }
        public RecordingEmailNotification Email { get; } = new();
        public FakePaymentProvider PaymentProvider { get; } = new();
        public SubscriptionBillingService Service { get; }

        public SubscriptionBillingFixture()
        {
            Environment.SetEnvironmentVariable("BASE_URL_CLIENT", "https://rendatop.sistemaonline.shop");

            var databaseName = Guid.NewGuid().ToString("N");
            _options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            using var context = new Context(_options);
            User = new User
            {
                name = "Hudson Ventura",
                email = "hudson@example.com",
                password = "secret",
                cpf = "12345678909"
            };
            context.users.Add(User);
            context.SaveChanges();

            Service = new SubscriptionBillingService(
                new TestContextFactory(_options),
                PaymentProvider,
                Email,
                NullLogger<SubscriptionBillingService>.Instance);
        }

        public Context CreateAssertionContext() => new(_options);

        public void SeedActiveSubscriptionWithCharge(string paymentMethod, DateTime currentPeriodEnd, DateTime? currentCycleReminderSentAt)
        {
            using var context = new Context(_options);
            var currentPeriodStart = currentPeriodEnd.AddMonths(-1);
            var subscription = new Subscription
            {
                user_id = User.id,
                plan_id = "plus",
                status = SubscriptionStatus.Active,
                payment_method = paymentMethod,
                mp_payment_id = "seed-payment",
                mp_customer_id = paymentMethod.Contains("card") ? "cust-seed" : null,
                mp_card_id = paymentMethod.Contains("card") ? "card-seed" : null,
                current_period_start = currentPeriodStart,
                current_period_end = currentPeriodEnd,
                created_at = DateTime.UtcNow.AddMonths(-1),
                updated_at = DateTime.UtcNow.AddMonths(-1)
            };
            context.subscriptions.Add(subscription);
            context.SaveChanges();

            context.subscription_charges.Add(new SubscriptionCharge
            {
                subscription_id = subscription.id,
                user_id = User.id,
                plan_id = "plus",
                payment_method = paymentMethod,
                amount = 6.9m,
                payer_cpf = User.cpf,
                provider_payment_id = "seed-payment",
                provider_external_reference = "seed-ext-ref",
                status = SubscriptionChargeStatus.Approved,
                charge_kind = SubscriptionChargeKind.Initial,
                billing_period_start = currentPeriodStart,
                billing_period_end = currentPeriodEnd,
                due_at = currentPeriodEnd,
                approved_at = currentPeriodStart,
                reminder_sent_at = currentCycleReminderSentAt,
                receipt_sent_at = currentPeriodStart,
                created_at = currentPeriodStart,
                updated_at = currentPeriodStart
            });

            context.SaveChanges();
        }

        public void UpdateSubscription(Action<Subscription> update)
        {
            using var context = new Context(_options);
            var subscription = context.subscriptions.Single();
            update(subscription);
            context.SaveChanges();
        }

        public void SeedPendingRenewalCharge(DateTime currentPeriodEnd)
        {
            using var context = new Context(_options);
            var subscription = context.subscriptions.Single();
            context.subscription_charges.Add(new SubscriptionCharge
            {
                subscription_id = subscription.id,
                user_id = User.id,
                plan_id = "plus",
                payment_method = subscription.payment_method,
                amount = 6.9m,
                payer_cpf = User.cpf,
                provider_payment_id = "pending-renewal",
                provider_external_reference = "renewal-ext-ref",
                status = SubscriptionChargeStatus.Pending,
                charge_kind = SubscriptionChargeKind.Renewal,
                billing_period_start = subscription.current_period_end,
                billing_period_end = subscription.current_period_end.AddMonths(1),
                due_at = subscription.current_period_end,
                created_at = subscription.current_period_end.AddDays(-1),
                updated_at = subscription.current_period_end.AddDays(-1)
            });
            context.SaveChanges();
        }

        public void Dispose()
        {
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

    private sealed class RecordingEmailNotification : IEmailNotification
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task Notify(string toEmail, string title, string message, bool isHtml = false)
        {
            Messages.Add(new EmailMessage(toEmail, title, message, isHtml));
            return Task.CompletedTask;
        }
    }

    private sealed record EmailMessage(string ToEmail, string Title, string Message, bool IsHtml);

    private sealed class FakePaymentProvider : IPaymentProvider
    {
        public PaymentResult CardResult { get; set; } = new() { payment_id = "card-default", status = "approved", amount = 6.9m, approved_at = DateTime.UtcNow };
        public PaymentResult PixResult { get; set; } = new() { payment_id = "pix-default", status = "pending", amount = 6.9m };
        public PaymentResult BoletoResult { get; set; } = new() { payment_id = "boleto-default", status = "pending", amount = 6.9m };
        public PaymentResult SavedCardPaymentResult { get; set; } = new() { payment_id = "saved-card-default", status = "approved", amount = 6.9m, approved_at = DateTime.UtcNow };
        public (string customerId, string cardId) SavedCardResult { get; set; } = ("cust-default", "card-default");
        public Dictionary<string, PaymentResult> PaymentStatusResults { get; } = [];
        public List<RefundRequest> RefundRequests { get; } = [];

        public Task<PaymentResult> CreateCardPaymentAsync(CardPaymentRequest request) => Task.FromResult(Clone(CardResult));
        public Task<PaymentResult> CreatePixPaymentAsync(PixPaymentRequest request) => Task.FromResult(Clone(PixResult));
        public Task<PaymentResult> CreateBoletoPaymentAsync(BoletoPaymentRequest request) => Task.FromResult(Clone(BoletoResult));
        public Task<PaymentResult> GetPaymentStatusAsync(string paymentId) => Task.FromResult(Clone(PaymentStatusResults[paymentId]));
        public Task<(string customerId, string cardId)> SaveCardAsync(string cardToken, string email) => Task.FromResult(SavedCardResult);
        public Task<PaymentResult> CreateSavedCardPaymentAsync(SavedCardPaymentRequest request) => Task.FromResult(Clone(SavedCardPaymentResult));
        public Task<PaymentRefundResult> RefundPaymentAsync(string paymentId, decimal? amount = null, CancellationToken cancellationToken = default)
        {
            RefundRequests.Add(new RefundRequest(paymentId, amount));
            return Task.FromResult(new PaymentRefundResult
            {
                refund_id = $"refund-{paymentId}",
                status = "approved",
                amount = amount,
                created_at = DateTime.UtcNow
            });
        }

        private static PaymentResult Clone(PaymentResult source)
        {
            return new PaymentResult
            {
                status = source.status,
                status_detail = source.status_detail,
                payment_id = source.payment_id,
                payment_method = source.payment_method,
                amount = source.amount,
                approved_at = source.approved_at,
                date_of_expiration = source.date_of_expiration.HasValue
                    ? source.date_of_expiration.Value.ToUniversalTime()
                    : null,
                pix_qr_code = source.pix_qr_code,
                pix_qr_code_base64 = source.pix_qr_code_base64,
                boleto_barcode_content = source.boleto_barcode_content,
                boleto_barcode_image_base64 = source.boleto_barcode_image_base64,
                boleto_digitable_line = source.boleto_digitable_line,
                boleto_url = source.boleto_url,
                customer_id = source.customer_id,
                card_id = source.card_id
            };
        }

        public sealed record RefundRequest(string PaymentId, decimal? Amount);
    }
}
