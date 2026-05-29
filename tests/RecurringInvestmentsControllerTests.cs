using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using server.Controllers;
using server.Domain;
using server.RequestObjects;
using server.Utils;

namespace tests;

public class RecurringInvestmentsControllerTests
{
    [Fact]
    public void Create_PersistsRecurringInvestmentForPaidPlan()
    {
        using var fixture = new RecurringInvestmentsControllerFixture();
        fixture.SeedActiveSubscription("plus");
        var request = fixture.CreateRequest();

        var result = fixture.Controller.Create(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RecurringInvestmentResponse>(okResult.Value);

        Assert.Equal("Aporte recorrente CDI", response.title);
        Assert.Equal((int)fixture.Bank.Code, response.bank_code);
        Assert.Equal(RecurringInvestmentFrequency.Monthly, response.frequency);
        Assert.Empty(response.weekdays);
        Assert.Equal([1, 6, 12], response.months);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedRecurring = assertionContext.recurring_investments
            .Include(item => item.bank)
            .Single();

        Assert.Equal(fixture.User.id, savedRecurring.owner_id);
        Assert.Equal(fixture.Bank.Id, savedRecurring.bank_id);
        Assert.Equal("Aporte recorrente CDI", savedRecurring.title);
        Assert.Equal("1,6,12", savedRecurring.months_csv);
        Assert.True(savedRecurring.active);
    }

    [Fact]
    public void Create_ThrowsWhenUserDoesNotHavePaidPlan()
    {
        using var fixture = new RecurringInvestmentsControllerFixture();

        var exception = Assert.Throws<ExpectedException>(() =>
            fixture.Controller.Create(fixture.CreateRequest()));

        Assert.Equal("Investimentos recorrentes exigem um plano pago ativo.", exception.Message);

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Empty(assertionContext.recurring_investments);
    }

    [Fact]
    public void Get_ReturnsOnlyAuthenticatedUserRecurringInvestments()
    {
        using var fixture = new RecurringInvestmentsControllerFixture();
        fixture.SeedActiveSubscription("plus");
        fixture.SeedRecurringInvestment();
        fixture.SeedRecurringInvestmentForAnotherUser();

        var result = fixture.Controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RecurringInvestmentsOverviewResponse>(okResult.Value);

        Assert.True(response.recurring_investments_enabled);
        var item = Assert.Single(response.items);
        Assert.Equal("Aporte recorrente CDI", item.title);
        Assert.Equal((int)fixture.Bank.Code, item.bank_code);
    }

    [Fact]
    public void Create_PersistsMultipleWeekdaysForWeeklyRecurringInvestment()
    {
        using var fixture = new RecurringInvestmentsControllerFixture();
        fixture.SeedActiveSubscription("plus");
        var request = fixture.CreateRequest(frequency: RecurringInvestmentFrequency.Weekly);

        var result = fixture.Controller.Create(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RecurringInvestmentResponse>(okResult.Value);

        Assert.Equal(RecurringInvestmentFrequency.Weekly, response.frequency);
        Assert.Equal([(short)1, (short)3, (short)5], response.weekdays);
        Assert.Empty(response.months);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedRecurring = assertionContext.recurring_investments.Single();
        Assert.Equal([(short)1, (short)3, (short)5], savedRecurring.weekdays);
        Assert.Equal(string.Empty, savedRecurring.months_csv);
    }

    [Fact]
    public void Create_ThrowsWhenWeeklyRecurringInvestmentHasNoWeekdays()
    {
        using var fixture = new RecurringInvestmentsControllerFixture();
        fixture.SeedActiveSubscription("plus");
        var request = fixture.CreateRequest(frequency: RecurringInvestmentFrequency.Weekly);
        request.weekdays = [];

        var exception = Assert.Throws<ExpectedException>(() => fixture.Controller.Create(request));

        Assert.Equal("Selecione pelo menos um dia da semana para a recorrência.", exception.Message);
    }

    [Fact]
    public void ToInvestmentRequest_AppendsPortugueseYearAndMonthToTitle()
    {
        using var fixture = new RecurringInvestmentsControllerFixture();
        var recurring = new RecurringInvestment(fixture.CreateRequest(title: "Aporte recorrente CDI"), fixture.User, fixture.Bank);

        var investmentRequest = recurring.ToInvestmentRequest(new DateOnly(2026, 6, 6));

        Assert.Equal("Aporte recorrente CDI 2026/Junho", investmentRequest.title);
    }

    [Fact]
    public void Create_GeneratesImmediateInvestmentWithLocalizedSuffix_WhenRecurringMatchesToday()
    {
        using var fixture = new RecurringInvestmentsControllerFixture();
        fixture.SeedActiveSubscription("plus");
        var today = DateTime.Now;
        var request = fixture.CreateRequest(
            title: "Aporte recorrente CDI",
            frequency: RecurringInvestmentFrequency.Monthly);
        request.day_of_month = today.Day;
        request.months = [today.Month];

        fixture.Controller.Create(request);

        using var assertionContext = fixture.CreateAssertionContext();
        var investment = Assert.Single(assertionContext.investments);
        var culture = new CultureInfo("pt-BR");
        var monthName = culture.TextInfo.ToTitleCase(today.ToString("MMMM", culture));
        var expectedSuffix = $"{today:yyyy}/{monthName}";
        Assert.Equal($"Aporte recorrente CDI {expectedSuffix}", investment.title);
    }

    private sealed class RecurringInvestmentsControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public RecurringInvestmentsController Controller { get; }
        public User User { get; }
        public Bank Bank { get; }

        public RecurringInvestmentsControllerFixture()
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
                password = "secret"
            };
            Bank = new Bank
            {
                Code = 1,
                Name = "Banco Teste",
                CompanyName = "Banco Teste S.A.",
                Cnpj = "00000000000191"
            };

            Context.users.Add(User);
            Context.banks.Add(Bank);
            Context.SaveChanges();

            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = User;
            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

            Controller = new RecurringInvestmentsController(
                httpContextAccessor,
                new TestContextFactory(_options));
        }

        public Context CreateAssertionContext() => new(_options);

        public void SeedActiveSubscription(string planId)
        {
            Context.subscriptions.Add(new Subscription
            {
                user_id = User.id,
                plan_id = planId,
                status = SubscriptionStatus.Active,
                payment_method = "credit_card",
                current_period_start = DateTime.UtcNow.AddDays(-1),
                current_period_end = DateTime.UtcNow.AddMonths(1)
            });
            Context.SaveChanges();
        }

        public RecurringInvestment SeedRecurringInvestment()
        {
            var recurring = new RecurringInvestment(CreateRequest(), User, Bank);
            Context.recurring_investments.Add(recurring);
            Context.SaveChanges();
            return recurring;
        }

        public RecurringInvestment SeedRecurringInvestmentForAnotherUser()
        {
            var anotherUser = new User
            {
                name = "Other User",
                email = "other@example.com",
                password = "secret"
            };

            Context.users.Add(anotherUser);
            Context.SaveChanges();

            var recurring = new RecurringInvestment(CreateRequest(title: "Recorrencia de outro usuario"), anotherUser, Bank);
            Context.recurring_investments.Add(recurring);
            Context.SaveChanges();
            return recurring;
        }

        public RecurringInvestmentRequest CreateRequest(
            string title = "Aporte recorrente CDI",
            RecurringInvestmentFrequency frequency = RecurringInvestmentFrequency.Monthly)
        {
            return new RecurringInvestmentRequest
            {
                title = title,
                investment_type = InvestmentType.CDB,
                bank_code = Bank.Code,
                value = 500m,
                index = IdexesType.CDI,
                index_percent = 110m,
                index_value = 0m,
                taxes = true,
                liquidity_daily = false,
                duration_days = 365,
                frequency = frequency,
                weekdays = frequency == RecurringInvestmentFrequency.Weekly ? [(short)1, (short)3, (short)5] : null,
                day_of_month = frequency == RecurringInvestmentFrequency.Monthly ? 15 : null,
                months = frequency == RecurringInvestmentFrequency.Monthly ? [1, 6, 12] : null,
                active = true
            };
        }

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
}
