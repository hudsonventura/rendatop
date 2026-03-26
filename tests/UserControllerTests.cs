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
            true,
            false
        ));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSettingsResponse>(okResult.Value);

        Assert.Equal("Nome Atualizado", response.name);
        Assert.Equal("updated@example.com", response.email);

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
            true,
            false
        )));

        Assert.Equal("Nome é obrigatório.", exception.Message);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == fixture.User.id);
        Assert.Equal("Test User", savedUser.name);
    }

    private sealed class UserControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public UserController Controller { get; }
        public User User { get; }

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

            Controller = new UserController(
                httpContextAccessor,
                new TestContextFactory(_options),
                new FakeNotification(),
                new FakeWhatsAppNotification(),
                new FakeEmailNotification());
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
        public Task Notify(string title, string message) => Task.CompletedTask;
    }

    private sealed class FakeWhatsAppNotification : IWhatsAppNotification
    {
        public Task Notify(string phone, string title, string message) => Task.CompletedTask;
    }

    private sealed class FakeEmailNotification : IEmailNotification
    {
        public Task Notify(string destination, string title, string message) => Task.CompletedTask;
    }
}
