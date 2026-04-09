using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Controllers;
using server.Domain;
using server.RequestObjects;

namespace tests;

public class LandingVisitsControllerTests
{
    [Fact]
    public void Register_PersistsVisitIpAndHeaders()
    {
        using var fixture = new LandingVisitsControllerFixture();

        fixture.Controller.ControllerContext = new ControllerContext
        {
            HttpContext = fixture.CreateHttpContext(
                remoteIpAddress: "10.0.0.8",
                forwardedFor: "177.10.20.30, 10.0.0.8",
                userAgent: "Mozilla/5.0 Test",
                referer: "https://instagram.com/rendatop")
        };

        var result = fixture.Controller.Register(new LandingVisitRequest("Instagram"));

        Assert.IsType<NoContentResult>(result);

        using var assertionContext = fixture.CreateAssertionContext();
        var visit = assertionContext.landing_visits.Single();

        Assert.Equal("instagram", visit.visit);
        Assert.Equal("177.10.20.30", visit.ip_address);
        Assert.Equal("Mozilla/5.0 Test", visit.user_agent);
        Assert.Equal("https://instagram.com/rendatop", visit.referrer);
    }

    [Fact]
    public void Register_UsesDirectWhenVisitIsMissing()
    {
        using var fixture = new LandingVisitsControllerFixture();

        fixture.Controller.ControllerContext = new ControllerContext
        {
            HttpContext = fixture.CreateHttpContext(remoteIpAddress: "192.168.0.15")
        };

        var result = fixture.Controller.Register(new LandingVisitRequest(null));

        Assert.IsType<NoContentResult>(result);

        using var assertionContext = fixture.CreateAssertionContext();
        var visit = assertionContext.landing_visits.Single();

        Assert.Equal("direct", visit.visit);
        Assert.Equal("192.168.0.15", visit.ip_address);
    }

    private sealed class LandingVisitsControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public LandingVisitsController Controller { get; }

        public LandingVisitsControllerFixture()
        {
            var databaseName = Guid.NewGuid().ToString("N");
            _options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            Context = new Context(_options);
            Controller = new LandingVisitsController(new TestContextFactory(_options));
        }

        public Context CreateAssertionContext() => new(_options);

        public DefaultHttpContext CreateHttpContext(
            string remoteIpAddress,
            string? forwardedFor = null,
            string? userAgent = null,
            string? referer = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIpAddress);

            if (!string.IsNullOrWhiteSpace(forwardedFor))
                httpContext.Request.Headers["X-Forwarded-For"] = forwardedFor;

            if (!string.IsNullOrWhiteSpace(userAgent))
                httpContext.Request.Headers.UserAgent = userAgent;

            if (!string.IsNullOrWhiteSpace(referer))
                httpContext.Request.Headers.Referer = referer;

            return httpContext;
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
