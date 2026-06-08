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
        Assert.NotNull(investment.table_calculated);
        Assert.NotEmpty(investment.table_calculated);
        Assert.NotEmpty(investment.redemptions);
    }

    [Fact]
    public void Get_BuildsTableDisplayValuesDiscountingRedemptions()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment(
            title: "Investimento com resgate",
            value: 1000m,
            dueDate: DateTime.UtcNow.Date.AddYears(1),
            archived: false,
            index: IdexesType.PERCENT_YEAR,
            indexPercent: 10m);

        fixture.Context.redemptions.Add(new Redemption(
            investment,
            new RedemptionRequest
            {
                title = "Resgate parcial",
                value = 100m,
                date = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            }));
        fixture.Context.SaveChanges();

        var returnedInvestment = Assert.Single(fixture.Controller.Get());
        var baseCalc = returnedInvestment.calculated.First();
        var tableCalc = returnedInvestment.table_calculated!.First();
        var expectedRatio = 100m / baseCalc.value_liq;

        Assert.Equal(1000m - (1000m * expectedRatio), returnedInvestment.table_value!.Value, 6);
        Assert.Equal(baseCalc.IOF_value - (baseCalc.IOF_value * expectedRatio), tableCalc.IOF_value, 6);
        Assert.Equal(baseCalc.IR_value - (baseCalc.IR_value * expectedRatio), tableCalc.IR_value, 6);
        Assert.Equal(baseCalc.profit_liq - (baseCalc.profit_liq * expectedRatio), tableCalc.profit_liq, 6);
        Assert.Equal(returnedInvestment.table_value.Value + tableCalc.profit_liq, tableCalc.value_liq, 6);
    }

    [Fact]
    public void Get_ArchivedInvestmentKeepsOriginalDisplayValues()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment(
            title: "Investimento arquivado",
            value: 1000m,
            dueDate: DateTime.UtcNow.Date.AddYears(1),
            archived: true,
            index: IdexesType.PERCENT_YEAR,
            indexPercent: 10m);

        fixture.Context.redemptions.Add(new Redemption(
            investment,
            new RedemptionRequest
            {
                title = "Resgate antigo",
                value = 100m,
                date = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            }));
        fixture.Context.SaveChanges();

        var returnedInvestment = Assert.Single(fixture.Controller.Get());
        var baseCalc = returnedInvestment.calculated.First();
        var tableCalc = returnedInvestment.table_calculated!.First();

        Assert.Equal(returnedInvestment.value, returnedInvestment.table_value);
        Assert.Equal(baseCalc.value_liq, tableCalc.value_liq, 6);
        Assert.Equal(baseCalc.profit_liq, tableCalc.profit_liq, 6);
        Assert.Equal(baseCalc.IR_value, tableCalc.IR_value, 6);
        Assert.Equal(baseCalc.IOF_value, tableCalc.IOF_value, 6);
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

    // [Fact]
    // public void Insert_BlocksAiAssistedSaveWhenMonthlyAiLimitIsReached()
    // {
    //     using var fixture = new InvestmentsControllerFixture();
    //     fixture.Context.ai_usages.AddRange(
    //         new AiUsage
    //         {
    //             user_id = fixture.User.id,
    //             user = fixture.User,
    //             feature = SubscriptionFeatureAccess.InvestmentDocumentExtractionFeature,
    //             provider = "openai",
    //             created_at = DateTime.UtcNow.AddDays(-1)
    //         },
    //         new AiUsage
    //         {
    //             user_id = fixture.User.id,
    //             user = fixture.User,
    //             feature = SubscriptionFeatureAccess.InvestmentDocumentExtractionFeature,
    //             provider = "openai",
    //             created_at = DateTime.UtcNow.AddDays(-2)
    //         });
    //     fixture.Context.SaveChanges();

    //     var request = fixture.CreateInvestmentRequest();
    //     request.ai_extracted = true;

    //     var exception = Assert.Throws<ExpectedException>(() => fixture.Controller.Insert(request));

    //     Assert.Equal(System.Net.HttpStatusCode.Forbidden, exception.StatusCode);
    // }

    [Fact]
    public void Insert_BlocksAiAssistedSaveWhenUserPlanDoesNotAllowReceipts()
    {
        using var fixture = new InvestmentsControllerFixture();
        var request = fixture.CreateInvestmentRequest();
        request.ai_extracted = true;

        var exception = Assert.Throws<ExpectedException>(() => fixture.Controller.Insert(request));

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Contains("Seu plano Free permite 0 leituras de comprovantes por mês", exception.Message);
        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Empty(assertionContext.ai_usages);
        Assert.Empty(assertionContext.investments);
    }

    [Fact]
    public void Insert_RecordsAiUsageAfterAiAssistedSaveWhenPlanAllowsReceipts()
    {
        using var fixture = new InvestmentsControllerFixture();
        fixture.SeedActiveSubscription("plus");
        var request = fixture.CreateInvestmentRequest();
        request.ai_extracted = true;

        var investmentId = fixture.Controller.Insert(request);

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.NotEqual(Guid.Empty, investmentId);
        var usage = Assert.Single(assertionContext.ai_usages);
        Assert.Equal(fixture.User.id, usage.user_id);
        Assert.Equal(SubscriptionFeatureAccess.InvestmentDocumentExtractionFeature, usage.feature);
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
    public void Redeem_ArchivesInvestmentWhenRedemptionConsumesCurrentLiquidValue()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment(
            value: 1000m,
            dueDate: DateTime.UtcNow.Date.AddYears(1),
            index: IdexesType.PERCENT_YEAR,
            indexPercent: 10m);

        var currentValueLiq = fixture.Controller.Get().Single().calculated.First().value_liq;

        fixture.Controller.Redeem(investment.id, new RedemptionRequest
        {
            title = "Resgate total",
            value = currentValueLiq,
            date = DateTime.UtcNow
        });

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.True(assertionContext.investments.Single(i => i.id == investment.id).archived);
    }

    [Fact]
    public void DeleteRedemption_RemovesRedemptionFromDatabase()
    {
        using var fixture = new InvestmentsControllerFixture();
        var investment = fixture.SeedInvestment();
        var redemption = new Redemption(
            investment,
            new RedemptionRequest
            {
                title = "Resgate errado",
                value = 200m,
                date = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            });

        fixture.Context.redemptions.Add(redemption);
        fixture.Context.SaveChanges();

        var result = fixture.Controller.DeleteRedemption(redemption.id);

        Assert.IsType<NoContentResult>(result);

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Empty(assertionContext.redemptions);
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

    private static FormFile BuildFormFile(string fileName, string contentType, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
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

        public void SeedActiveSubscription(string planId)
        {
            Context.subscriptions.Add(new Subscription
            {
                user = User,
                user_id = User.id,
                plan_id = planId,
                status = SubscriptionStatus.Active,
                payment_method = "test",
                current_period_start = DateTime.UtcNow.AddDays(-1),
                current_period_end = DateTime.UtcNow.AddMonths(1)
            });
            Context.SaveChanges();
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
        public Task Notify(string title, string message, string? chatId = null) => Task.CompletedTask;
    }

    private sealed class FakeInvestmentDocumentExtractor : IInvestmentDocumentExtractor
    {
        public int Calls { get; private set; }

        public Task<InvestmentDocumentExtractionResult> ExtractAsync(
            IFormFile file,
            IReadOnlyCollection<Bank> banks,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new InvestmentDocumentExtractionResult
            {
                title = "Extraido"
            });
        }
    }
}
