using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using server.Controllers;
using server.Domain;
using server.RequestObjects;
using server.Utils;

namespace tests;

public class InvestmentsControllerTests
{
    [Fact]
    public void Get_ReturnsOnlyAuthenticatedUserInvestmentsWithCalculatedValues()
    {
        using var fixture = new InvestmentsControllerFixture();
        var ownInvestment = fixture.SeedInvestment(
            title: "Meu investimento",
            value: 1000m,
            dueDate: DateTime.UtcNow.Date.AddYears(1), 
            archived: false,
            index: IdexesType.PERCENT_YEAR,
            indexPercent: 10m);
        fixture.SeedInvestmentForAnotherUser();

        var request = new RedemptionRequest
        {
            title = "Resgate parcial",
            value = 100m,
            date = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        var returnedInvestment = fixture.Controller.Redeem(ownInvestment.id, request);

        var result = fixture.Controller.Get();

        var investment = Assert.Single(result);
        Assert.Equal(ownInvestment.id, investment.id);
        Assert.Equal("Meu investimento", investment.title);
        Assert.NotNull(investment.bank);
        Assert.NotNull(investment.owner);
        Assert.NotNull(investment.calculated);
        Assert.NotEmpty(investment.calculated);
        Assert.NotEmpty(investment.redemptions);
    }

    [Fact]
    public void Insert_PersistsInvestmentForAuthenticatedUser()
    {
        using var fixture = new InvestmentsControllerFixture();
        var request = fixture.CreateInvestmentRequest();

        var investmentId = fixture.Controller.Insert(request);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedInvestment = assertionContext.investments
            .Include(i => i.owner)
            .Include(i => i.bank)
            .Single(i => i.id == investmentId);

        Assert.Equal(fixture.User.id, savedInvestment.owner.id);
        Assert.Equal("Novo investimento", savedInvestment.title);
        Assert.Equal(request.value, savedInvestment.value);
        Assert.Equal(fixture.Bank.Code, savedInvestment.bank.Code);
        Assert.False(savedInvestment.archived);
    }

    [Fact]
    public void Update_ChangesExistingInvestmentFields()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment();
        var request = fixture.CreateInvestmentRequest(
            title: "Investimento editado",
            value: 2750m,
            dueDate: DateTime.UtcNow.Date.AddMonths(6),
            archived: true);

        var result = fixture.Controller.Update(investment.id, request);

        Assert.IsType<OkResult>(result);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedInvestment = assertionContext.investments
            .Include(i => i.bank)
            .Single(i => i.id == investment.id);

        Assert.Equal("Investimento editado", savedInvestment.title);
        Assert.Equal(2750m, savedInvestment.value);
        Assert.Equal(request.date_expected_sell, savedInvestment.due_date);
        Assert.True(savedInvestment.archived);
    }

    [Fact]
    public void Redeem_CreatesRedemptionForInvestment()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment();
        var request = new RedemptionRequest
        {
            title = "Resgate parcial",
            value = 500m,
            date = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        var returnedInvestment = fixture.Controller.Redeem(investment.id, request);

        Assert.Equal(investment.id, returnedInvestment.id);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedRedemption = assertionContext.redemptions
            .Include(r => r.investment)
            .Single();

        Assert.Equal("Resgate parcial", savedRedemption.title);
        Assert.Equal(500m, savedRedemption.value);
        Assert.Equal(request.date, savedRedemption.date);
        Assert.Equal(investment.id, savedRedemption.investment.id);
    }

    [Fact]
    public void Archive_SetsArchivedFlagForMaturedInvestment()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment(dueDate: DateTime.UtcNow.Date.AddDays(-1));

        var result = fixture.Controller.Archive(investment.id, new ArchiveInvestmentRequest { archived = true });

        Assert.IsType<NoContentResult>(result);
        using var assertionContext = fixture.CreateAssertionContext();
        Assert.True(assertionContext.investments.Single(i => i.id == investment.id).archived);
    }

    [Fact]
    public void Delete_RemovesInvestmentFromDatabase()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment();

        fixture.Controller.Delete(investment.id);

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Empty(assertionContext.investments);
    }

    [Fact]
    public void Archive_ThrowsWhenInvestmentIsNotMatured()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment(dueDate: DateTime.UtcNow.Date.AddDays(5));

        var exception = Assert.Throws<ExpectedException>(() =>
            fixture.Controller.Archive(investment.id, new ArchiveInvestmentRequest { archived = true }));

        Assert.Equal("Somente investimentos vencidos podem ser arquivados.", exception.Message);
        using var assertionContext = fixture.CreateAssertionContext();
        Assert.False(assertionContext.investments.Single(i => i.id == investment.id).archived);
    }

    private sealed class InvestmentsControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public InvestmentsController Controller { get; }
        public User User { get; }
        public Bank Bank { get; }

        public InvestmentsControllerFixture()
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

            Controller = new InvestmentsController(
                NullLogger<InvestmentsController>.Instance,
                httpContextAccessor,
                new TestContextFactory(_options),
                new FakeNotification());
        }

        public Context CreateAssertionContext() => new(_options);

        public Investment SeedInvestment(
            string title = "Investimento inicial",
            decimal value = 1500m,
            DateTime? dueDate = null,
            bool archived = false,
            IdexesType index = IdexesType.CDI,
            decimal indexPercent = 110m)
        {
            var investment = new Investment(
                CreateInvestmentRequest(title, value, dueDate, archived, index, indexPercent),
                User,
                Bank);
            Context.investments.Add(investment);
            Context.SaveChanges();
            return investment;
        }

        public Investment SeedInvestmentForAnotherUser()
        {
            var anotherUser = new User
            {
                name = "Other User",
                email = "other@example.com",
                password = "secret"
            };

            Context.users.Add(anotherUser);
            Context.SaveChanges();

            var investment = new Investment(
                CreateInvestmentRequest(
                    title: "Investimento de outro usuario",
                    value: 500m,
                    dueDate: DateTime.UtcNow.Date.AddDays(10),
                    archived: false,
                    index: IdexesType.PERCENT_YEAR,
                    indexPercent: 10m),
                anotherUser,
                Bank);

            Context.investments.Add(investment);
            Context.SaveChanges();
            return investment;
        }

        public InvestmentRequest CreateInvestmentRequest(
            string title = "Novo investimento",
            decimal value = 2000m,
            DateTime? dueDate = null,
            bool archived = false,
            IdexesType index = IdexesType.CDI,
            decimal indexPercent = 110m)
        {
            return new InvestmentRequest
            {
                title = title,
                bank_code = Bank.Code,
                value = value,
                date_buy = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                date_expected_sell = dueDate,
                index = index,
                index_percent = indexPercent,
                index_value = 0m,
                taxes = true,
                archived = archived
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

    private sealed class FakeNotification : INotification
    {
        public Task Notify(string title, string message) => Task.CompletedTask;
    }
}
