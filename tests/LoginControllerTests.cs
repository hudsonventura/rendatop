using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using server.Controllers;
using server.Domain;
using server.Utils;
using StackExchange.Redis;

namespace tests;

public class LoginControllerTests
{
    [Fact]
    public async Task Signup_CreatesPendingUserAndSendsVerificationEmail()
    {
        using var fixture = new LoginControllerFixture();

        var result = await fixture.Controller.Signup(new SignUpRequest("Novo Usuario", "novo@example.com", "secret123"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SignupPendingResponse>(okResult.Value);

        Assert.Equal("novo@example.com", response.email);
        Assert.True(response.email_sent);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.email == "novo@example.com");
        Assert.False(savedUser.email_verified);
        Assert.Equal(UserType.Common, savedUser.user_type);
        Assert.Equal(AuthProvider.Password, savedUser.auth_provider);
        Assert.False(string.IsNullOrWhiteSpace(savedUser.email_verification_secret));
        Assert.NotNull(savedUser.email_verification_sent_at);

        var email = Assert.Single(fixture.Email.Messages);
        Assert.Equal("novo@example.com", email.ToEmail);
        Assert.Contains("Verificação de email", email.Title);
    }

    [Fact]
    public void VerifySignup_ActivatesUserAndCreatesSession()
    {
        using var fixture = new LoginControllerFixture();
        var secret = TotpUtility.GenerateBase32Secret();
        var user = fixture.SeedUser(
            email: "pending@example.com",
            password: "secret123",
            emailVerified: false,
            emailVerificationSecret: secret,
            emailVerificationSentAt: DateTime.UtcNow);

        var code = TotpUtility.GenerateCode(secret, periodSeconds: 300, digits: 6);

        var result = fixture.Controller.VerifySignup(new SignupVerificationRequest(user.email, code));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);

        Assert.Equal(user.email, response.email);
        Assert.Equal(UserType.Common, response.user_type);
        Assert.Single(fixture.DatabaseStringSetAsyncCalls);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == user.id);
        Assert.True(savedUser.email_verified);
        Assert.Null(savedUser.email_verification_secret);
        Assert.Null(savedUser.email_verification_sent_at);
    }

    [Fact]
    public void Login_RejectsUnverifiedUser()
    {
        using var fixture = new LoginControllerFixture();
        fixture.SeedUser(
            email: "pending@example.com",
            password: "secret123",
            emailVerified: false,
            emailVerificationSecret: TotpUtility.GenerateBase32Secret(),
            emailVerificationSentAt: DateTime.UtcNow);

        var exception = Assert.Throws<ExpectedException>(() =>
            fixture.Controller.Login(new LoginRecord("pending@example.com", "secret123")));

        Assert.Equal("Sua conta ainda não foi ativada. Verifique o código enviado para seu email antes de entrar.", exception.Message);
    }

    [Fact]
    public async Task ResendSignupVerification_RotatesSecretAndSendsEmail()
    {
        using var fixture = new LoginControllerFixture();
        var user = fixture.SeedUser(
            email: "pending@example.com",
            password: "secret123",
            emailVerified: false,
            emailVerificationSecret: TotpUtility.GenerateBase32Secret(),
            emailVerificationSentAt: DateTime.UtcNow.AddMinutes(-2));
        var originalSecret = user.email_verification_secret;

        var result = await fixture.Controller.ResendSignupVerification(new SignupVerificationResendRequest(user.email));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PasswordResetRequestResponse>(okResult.Value);
        Assert.Equal("Novo código de verificação enviado para seu email.", response.message);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedUser = assertionContext.users.Single(x => x.id == user.id);
        Assert.False(string.IsNullOrWhiteSpace(savedUser.email_verification_secret));
        Assert.NotEqual(originalSecret, savedUser.email_verification_secret);
        Assert.NotNull(savedUser.email_verification_sent_at);
        Assert.Single(fixture.Email.Messages);
    }

    private sealed class LoginControllerFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public LoginController Controller { get; }
        public RecordingEmailNotification Email { get; }
        public List<(RedisKey Key, RedisValue Value)> DatabaseStringSetAsyncCalls { get; }

        public LoginControllerFixture()
        {
            var databaseName = Guid.NewGuid().ToString("N");
            _options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            Environment.SetEnvironmentVariable("VITE_JWT_KEY", "tests-super-secret-key-1234567890");
            Context = new Context(_options);
            Email = new RecordingEmailNotification();
            var database = DispatchProxy.Create<IDatabase, RecordingDatabaseProxy>();
            DatabaseStringSetAsyncCalls =
                GetProxyProperty<List<(RedisKey Key, RedisValue Value)>>(database, nameof(RecordingDatabaseProxy.StringSetAsyncCalls));

            var muxer = DispatchProxy.Create<IConnectionMultiplexer, RecordingConnectionMultiplexerProxy>();
            SetProxyProperty(muxer, nameof(RecordingConnectionMultiplexerProxy.Database), database);

            Controller = new LoginController(
                new TestContextFactory(_options),
                muxer,
                new FakeEnvironment(),
                Email);

            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        public Context CreateAssertionContext() => new(_options);

        public User SeedUser(
            string email,
            string password,
            bool emailVerified = true,
            string? emailVerificationSecret = null,
            DateTime? emailVerificationSentAt = null)
        {
            var user = new User
            {
                name = "Test User",
                email = email,
                password = password,
                email_verified = emailVerified,
                email_verification_secret = emailVerificationSecret,
                email_verification_sent_at = emailVerificationSentAt
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

    private sealed class RecordingEmailNotification : IEmailNotification
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task Notify(string toEmail, string title, string message, bool isHtml = false)
        {
            Messages.Add(new EmailMessage(toEmail, title, message));
            return Task.CompletedTask;
        }
    }

    private sealed record EmailMessage(string ToEmail, string Title, string Message);

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private class RecordingConnectionMultiplexerProxy : DispatchProxy
    {
        public IDatabase? Database { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IConnectionMultiplexer.GetDatabase))
                return Database;

            return GetDefaultValue(targetMethod?.ReturnType);
        }
    }

    private static T GetProxyProperty<T>(object proxy, string propertyName)
    {
        var property = proxy.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Propriedade {propertyName} não encontrada no proxy.");

        return (T)(property.GetValue(proxy)
            ?? throw new InvalidOperationException($"Propriedade {propertyName} não foi inicializada."));
    }

    private static void SetProxyProperty(object proxy, string propertyName, object value)
    {
        var property = proxy.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Propriedade {propertyName} não encontrada no proxy.");

        property.SetValue(proxy, value);
    }

    private class RecordingDatabaseProxy : DispatchProxy
    {
        public List<(RedisKey Key, RedisValue Value)> StringSetAsyncCalls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                return null;

            if (targetMethod.Name == nameof(IDatabase.StringSetAsync))
            {
                StringSetAsyncCalls.Add(((RedisKey)args![0]!, (RedisValue)args[1]!));
                return Task.FromResult(true);
            }

            if (targetMethod.Name == nameof(IDatabase.StringSet))
                return true;

            if (targetMethod.Name == nameof(IDatabase.StringGet))
                return RedisValue.Null;

            if (targetMethod.Name == nameof(IDatabase.KeyDelete))
                return true;

            if (targetMethod.Name == nameof(IDatabase.KeyDeleteAsync))
                return Task.FromResult(true);

            return GetDefaultValue(targetMethod.ReturnType);
        }
    }

    private static object? GetDefaultValue(Type? type)
    {
        if (type is null || type == typeof(void))
            return null;

        if (type == typeof(Task))
            return Task.CompletedTask;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = type.GetGenericArguments()[0];
            var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [defaultValue]);
        }

        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return null;
    }
}
