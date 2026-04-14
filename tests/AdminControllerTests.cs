using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Controllers;
using server.Domain;
using server.Utils;

namespace tests;

public class AdminControllerTests
{
    [Fact]
    public void GetStats_ThrowsForCommonUser()
    {
        using var fixture = new AdminControllerFixture(UserType.Common);

        var exception = Assert.Throws<ExpectedException>(() => fixture.Controller.GetStats());

        Assert.Equal("Acesso permitido apenas para administradores.", exception.Message);
    }

    [Fact]
    public void GetStats_ReturnsCountsForPlansAuthProvidersAndVisits()
    {
        using var fixture = new AdminControllerFixture(UserType.Admin);

        var freeUser = fixture.SeedUser("free@example.com", AuthProvider.Password);
        var plusUser = fixture.SeedUser("plus@example.com", AuthProvider.Google);
        var proUser = fixture.SeedUser("pro@example.com", AuthProvider.Microsoft);

        fixture.Context.subscriptions.Add(new Subscription
        {
            user_id = plusUser.id,
            plan_id = "plus",
            status = SubscriptionStatus.Active,
            created_at = DateTime.UtcNow.AddDays(-2),
            updated_at = DateTime.UtcNow.AddDays(-2),
            current_period_start = DateTime.UtcNow.AddDays(-2),
            current_period_end = DateTime.UtcNow.AddDays(28)
        });

        fixture.Context.subscriptions.Add(new Subscription
        {
            user_id = proUser.id,
            plan_id = "pro",
            status = SubscriptionStatus.Active,
            created_at = DateTime.UtcNow.AddDays(-1),
            updated_at = DateTime.UtcNow.AddDays(-1),
            current_period_start = DateTime.UtcNow.AddDays(-1),
            current_period_end = DateTime.UtcNow.AddDays(29)
        });

        fixture.Context.landing_visits.AddRange(
            new LandingVisit { visit = "google" },
            new LandingVisit { visit = "google" },
            new LandingVisit { visit = "instagram" });
        fixture.Context.SaveChanges();

        var result = fixture.Controller.GetStats();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AdminStatsResponse>(okResult.Value);

        Assert.Equal(4, response.total_users);
        Assert.Equal(2, response.auth_provider_counts.without_sso);
        Assert.Equal(1, response.auth_provider_counts.google);
        Assert.Equal(1, response.auth_provider_counts.microsoft);

        Assert.Equal(2, response.users_by_plan.Single(x => x.plan_id == "free").users_count);
        Assert.Equal(1, response.users_by_plan.Single(x => x.plan_id == "plus").users_count);
        Assert.Equal(1, response.users_by_plan.Single(x => x.plan_id == "pro").users_count);

        Assert.Equal(2, response.visits_by_origin.Single(x => x.visit == "google").visits_count);
        Assert.Equal(1, response.visits_by_origin.Single(x => x.visit == "instagram").visits_count);
    }

    private sealed class AdminControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public AdminController Controller { get; }
        public User CurrentUser { get; }

        public AdminControllerFixture(UserType currentUserType)
        {
            var databaseName = Guid.NewGuid().ToString("N");
            _options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            Context = new Context(_options);
            CurrentUser = new User
            {
                name = "Admin User",
                email = "admin@example.com",
                password = "secret",
                user_type = currentUserType,
                auth_provider = AuthProvider.Password
            };

            Context.users.Add(CurrentUser);
            Context.SaveChanges();

            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = CurrentUser;
            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

            Controller = new AdminController(
                httpContextAccessor,
                new TestContextFactory(_options));
        }

        public User SeedUser(string email, AuthProvider authProvider)
        {
            var user = new User
            {
                name = email,
                email = email,
                password = "secret",
                user_type = UserType.Common,
                auth_provider = authProvider
            };

            Context.users.Add(user);
            Context.SaveChanges();
            return user;
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
