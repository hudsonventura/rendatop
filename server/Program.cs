using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DotNetEnv;
using Lib.AspNetCore.WebPush;
using Lib.Net.Http.WebPush;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using LogCenter;
using LogCenter.RequestInterceptor;
using server.BackgroundServices;
using server.Domain;
using server.Middlewares;
using server.Services;
using server.Utils;
using StackExchange.Redis;


Env.Load();

//tenta carregar o .env no diretorio pai, se houver
Env.Load("../.env");




SnowflakeGuid.SetMachineID(0);


var builder = WebApplication.CreateBuilder(args);


//LogCenter configuration
InterceptorOptions options = new InterceptorOptions(){
    // LogCenter's URL
    Url = Environment.GetEnvironmentVariable("LOGCENTER_URL") ?? string.Empty,

    // Table name 
    Table = Environment.GetEnvironmentVariable("LOGCENTER_TABLE") ?? string.Empty,

    // Generate this on LogCenter inteface, on you profile photo.
    Token = Environment.GetEnvironmentVariable("LOGCENTER_TOKEN") ?? string.Empty,
    
    BannedEventNames =
    {
        "ExecutingEndpoint",
        "Microsoft.EntityFrameworkCore.Database.Command.CommandExecutedExecuted",
        "ControllerActionExecuting",
        "ExecutedEndpoint",
        "ObjectResultExecuting",
        "PolicySuccess",
        "ActionExecuted",
    },
    BannedMessages =
    {
        "Request finished {Protocol} {Method} {Scheme}://{Host}{PathBase}{Path}{QueryString} - {StatusCode} {ContentLength} {ContentType} {ElapsedMilliseconds}ms",
    }

};
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddLogCenter(options);
});

// Remove default logging (ILogger, console, debug, etc)
builder.Logging.ClearProviders();
// Add LogCenter provider, to use as ILogger in the controllers and other services.
builder.Logging.AddLogCenter(options);
var logger = loggerFactory.CreateLogger<Program>();


builder.Services.AddSingleton<INotification>(provider =>
    new server.Utils.Telegram(
        provider.GetRequiredService<ILogger<server.Utils.Telegram>>(),
        Environment.GetEnvironmentVariable("TELEGRAM_TOKEN") ?? string.Empty,
        Environment.GetEnvironmentVariable("TELEGRAM_CHATID")));
builder.Services.AddSingleton<IWhatsAppNotification>(provider =>
    new FallbackWhatsAppNotification(
        provider.GetRequiredService<ILogger<FallbackWhatsAppNotification>>(),
        Environment.GetEnvironmentVariable("WHATSAPP_PROVIDER"),
        Environment.GetEnvironmentVariable("WHATSAPP_PROVIDER_FALLBACK"),
        new WWebJsWhatsAppNotification(
            provider.GetRequiredService<ILogger<WWebJsWhatsAppNotification>>(),
            Environment.GetEnvironmentVariable("WHATSAPP_WWEBJS_URL"),
            Environment.GetEnvironmentVariable("WHATSAPP_WWEBJS_API_KEY"),
            Environment.GetEnvironmentVariable("WHATSAPP_WWEBJS_SESSION_ID")),
        new WhatsApp(
            provider.GetRequiredService<ILogger<WhatsApp>>(),
            Environment.GetEnvironmentVariable("WHATSAPP_EVOLUTION_URL"),
            Environment.GetEnvironmentVariable("WHATSAPP_EVOLUTION_INSTANCE"),
            Environment.GetEnvironmentVariable("WHATSAPP_EVOLUTION_API_KEY"))));
builder.Services.AddSingleton<IEmailNotification>(provider =>
    new EmailSmtp(
        provider.GetRequiredService<ILogger<EmailSmtp>>(),
        Environment.GetEnvironmentVariable("SMTP_HOST"),
        Environment.GetEnvironmentVariable("SMTP_PORT"),
        Environment.GetEnvironmentVariable("SMTP_USERNAME"),
        Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
        Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL"),
        Environment.GetEnvironmentVariable("SMTP_FROM_NAME"),
        Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL")));
//_notify.Notify("Vencimento", $"Tem investimento vencendo hj carai, que tal resgata-lo? {Environment.NewLine} {investments[0].bank} {Environment.NewLine} {investments[0].title}");




logger.LogInformation("Starting web application");


builder.Services.AddMemoryCache();
builder.Services.AddMemoryVapidTokenCache();
builder.Services.AddPushServiceClient(options =>
{
    options.Subject = Environment.GetEnvironmentVariable("WEB_PUSH_SUBJECT");
    options.PublicKey = Environment.GetEnvironmentVariable("WEB_PUSH_PUBLIC_KEY");
    options.PrivateKey = Environment.GetEnvironmentVariable("WEB_PUSH_PRIVATE_KEY");
});
builder.Services.AddSingleton<IBrowserPushNotification>(provider =>
    new BrowserPushNotification(
        provider.GetRequiredService<ILogger<BrowserPushNotification>>(),
        provider.GetRequiredService<PushServiceClient>(),
        Environment.GetEnvironmentVariable("WEB_PUSH_PUBLIC_KEY"),
        Environment.GetEnvironmentVariable("WEB_PUSH_PRIVATE_KEY")));

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddHttpContextAccessor(); //usado para injetar o IHttpContextAccessor no construtor dos controllers
builder.Services.AddHttpClient();
builder.Services.AddScoped<OpenAiInvestmentDocumentExtractor>();
builder.Services.AddScoped<IInvestmentDocumentExtractor, InvestmentDocumentExtractorRouter>();
builder.Services.AddScoped<SubscriptionBillingService>();
builder.Services.AddScoped<ISocialPostPublisher, FacebookPostPublisher>();
builder.Services.AddScoped<ISocialPostPublisher, InstagramPostPublisher>();
builder.Services.AddScoped<ISocialPostPublisher, LinkedInPostPublisher>();
builder.Services.AddScoped<IBlogSocialPublisher, CompositeSocialPostPublisher>();
builder.Services.AddSingleton<ITemporarySocialAssetService, TemporarySocialAssetService>();


// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "RendaTop - Controle de Investimentos";
    config.Description = "Sistema de controle de Investimentos";
    config.Version = "v1";
    config.AddSecurity("Bearer", Enumerable.Empty<string>(), new NSwag.OpenApiSecurityScheme(){
        BearerFormat = "JWT",
        Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
        Scheme = "bearer",
        Name = "Authorization",
        In = NSwag.OpenApiSecurityApiKeyLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
        
    });
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Somente_Admin", policy =>
    {
        policy.RequireClaim("Sou admin", "verdade");
    });
});


// Configurar a autenticação
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "SeuIssuer",
            ValidAudience = "SeuAudience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("VITE_JWT_KEY")))
        };
        options.Events = new JwtBearerEvents
        {
            // Allow [Authorize] to read the JWT from the HttpOnly cookie
            // Prefer Authorization header (Scalar/Swagger still works), fall back to cookie
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                    context.Token = context.Request.Cookies["jwt"];
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var errorResponse = new Error()
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = string.IsNullOrEmpty(context.ErrorDescription) ? "Token de autenticação ausente ou inválido." : context.ErrorDescription
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status500InternalServerError;
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync("Muitas requisições em pouco tempo. Tente novamente em instantes.");
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (IsAuthenticatedForRateLimit(httpContext))
        {
            string partitionKey = $"auth:{GetAuthenticatedPartitionKey(httpContext)}";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
        }

        if (IsLoginOrSignupEndpoint(httpContext.Request.Path))
        {
            string partitionKey = $"anon-auth:{GetIpAddress(httpContext)}";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetNoLimiter("no-limit");
    });
});

DepenciesInjection();

// Payment provider
builder.Services.AddSingleton<server.Payments.IPaymentProvider, server.Payments.MercadoPago.MercadoPagoPaymentProvider>();

AddBackgroundServices();



var app = builder.Build();

// CORS — requires explicit origins when credentials (cookies) are used
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? new[] { "http://localhost:5173", "http://localhost:5174" };

app.UseCors(builder =>
    {
        builder.WithOrigins(corsOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
app.Use(async (context, next) =>
{
    context.Response.Headers["Referrer-Policy"] = "no-referrer"; // Altere para 'strict-origin', 'origin', etc., conforme necessário
    await next();
});

// Registra o middleware de exceção global
app.UseMiddleware<server.Middlewares.GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(options =>
    {
        options.Path = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference("/scalar", options =>
    {
        options.WithTitle("RendaTop - Controle de Investimentos API");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
        options.AddDocument("v1");
    });}

app.UseHttpsRedirection();

// Ativar autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseAuthenticationMiddleware();
app.UseRateLimiter();

// Use the interceptor LogCenter to log request and response
app.UseRequestInterceptor();


MigrateDatabase();



app.Run();









bool DepenciesInjection()
{

    string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? string.Empty;
    string port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? string.Empty;
    string db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? string.Empty;
    string user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? string.Empty;
    string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? string.Empty;

    string connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password}";

    Console.WriteLine(connectionString);
    if (string.IsNullOrEmpty(host)) return false; //em caso de não conseguir obter o host do banco, retorna false e impede que o app inicie. //TODO: Melhorar o retorno do erro
    //    throw new Exception($"Is the .env file in the correct place? I cannot read POSTGRES_HOST. I'm looking at {Environment.CurrentDirectory}");

    builder.Services.AddDbContextFactory<Context>(options =>
    {
        options.UseNpgsql(connectionString);
        //options.EnableSensitiveDataLogging();
        //options.EnableDetailedErrors();
    });


    string redis_host = Environment.GetEnvironmentVariable("REDIS_HOST");
    string redis_pass = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect($"{redis_host},password={redis_pass},abortConnect=false"));

    return true;
}


void AddBackgroundServices()
{
    //serviço que busca as taxas Selic do Banco Central
    builder.Services.AddHostedService<SelicBackgroundService>(provider =>
    {
        var contextFactory = provider.GetRequiredService<IDbContextFactory<Context>>();
        var context = contextFactory.CreateDbContext();
        var logger = provider.GetRequiredService<ILogger<SelicBackgroundService>>();
        return new SelicBackgroundService(logger, context);
    });

    //serviço que busca as taxas IPCA do Banco Central
    builder.Services.AddHostedService<IPCABackgroundService>(provider =>
    {
        var contextFactory = provider.GetRequiredService<IDbContextFactory<Context>>();
        var context = contextFactory.CreateDbContext();
        var logger = provider.GetRequiredService<ILogger<IPCABackgroundService>>();
        return new IPCABackgroundService(logger, context);
    });

    // serviço diário (06:00) para notificar investimentos com vencimento amanhã
    builder.Services.AddHostedService<DueTomorrowNotificationBackgroundService>();

    // serviço que monitora assinaturas a cada 6h (renovação automática, expiração, etc.)
    builder.Services.AddHostedService<SubscriptionMonitorBackgroundService>();

    // serviço diário (06:00 UTC) para gerar investimentos recorrentes
    builder.Services.AddHostedService<RecurringInvestmentsBackgroundService>();

    // serviço para responder rapidamente ao /start do bot e informar o chatID do usuário
    builder.Services.AddHostedService<TelegramBotBackgroundService>();
}



void MigrateDatabase(){

    logger.LogInformation("Applying migrations");
    using (var scope = app.Services.CreateScope())
    {
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Context>>();
        using var dbContext = dbContextFactory.CreateDbContext();
        try
        {
            dbContext.Database.Migrate();
            logger.LogInformation("Migrations applied successfully.");
        }
        catch (System.Exception error)
        {
            logger.LogError(error, "Não foi possível aplicar as migrations de forma automática.");
            throw;
        }

        


        var user = new User
        {
            id = SnowflakeGuid.Parse("0cd8827c-79c3-43e1-8d98-b5ed55259e03").Guid,
                name = "Test User",
                email = "example@example.com",
                password = "MinhaSenhaSuperSecreta"
            };
        if (!dbContext.Set<User>().Any())
        {
            
            dbContext.Set<User>().Add(user);
            dbContext.SaveChanges();
        }
        else
        {
            user = dbContext.Set<User>().OrderBy(item => item.id).First();
        }

        WalletAccess.EnsureDefaultWallet(dbContext, user);

        // Seed banks first so investments can reference them
        if (!dbContext.Set<Bank>().Any())
        {
            List<Bank> banks = new List<Bank>();
            banks.AddRange(
                new Bank
                {
                    Id = SnowflakeGuid.NewGuid(),
                    Name = "Sofisa",
                    CompanyName = "Banco Sofisa S.A.",
                    Cnpj = "977878978678",
                    Code = 876,
                    Color = "#22c55e"
                },
                new Bank
                {
                    Id = SnowflakeGuid.NewGuid(),
                    Name = "Banco Inter",
                    CompanyName = "Banco Inter S.A.",
                    Cnpj = "977878978678",
                    Code = 77,
                    Color = "#f97316"
                },
                new Bank
                {
                    Id = SnowflakeGuid.NewGuid(),
                    Name = "C6 Bank",
                    CompanyName = "C6 Bank S.A.",
                    Cnpj = "977878978678",
                    Code = 336,
                    Color = "#3b82f6"
                }
            );
            foreach (var item in banks)
            {
                dbContext.Set<Bank>().Add(item);
            }
            dbContext.SaveChanges();
        }

        //adiciona um investimento
        if (!dbContext.Set<Investment>().Any())
        {
            var sofisa   = dbContext.Set<Bank>().First(b => b.Name == "Sofisa");
            var inter    = dbContext.Set<Bank>().First(b => b.Name == "Banco Inter");
            var c6       = dbContext.Set<Bank>().First(b => b.Name == "C6 Bank");

            List<Investment> investments = new List<Investment>();
            investments.AddRange(
                new Investment
                {
                    id = SnowflakeGuid.NewGuid(),
                    owner = user,
                    bank = sofisa,
                    title = "13o parte 2 - LCA PRE - Banco ABC Brasil SA",
                    value = 2000,
                    date_buy = new DateTime(2024, 12, 20).ToUniversalTime(),
                    due_date = new DateTime(2025, 12, 16).ToUniversalTime(),
                    index = IdexesType.PERCENT_YEAR,
                    index_percent = 13.5m,
                    taxes = false
                },
                new Investment
                {
                    id = SnowflakeGuid.NewGuid(),
                    owner = user,
                    bank = sofisa,
                    title = "Reinvestimento. Não sei o que era",
                    value = 1470,
                    date_buy = new DateTime(2024, 12, 20).ToUniversalTime(),
                    due_date = new DateTime(2026, 06, 22).ToUniversalTime(),
                    index = IdexesType.PERCENT_YEAR,
                    index_percent = 14.9m,
                    taxes = true
                },
                new Investment
                {
                    id = SnowflakeGuid.NewGuid(),
                    owner = user,
                    bank = inter,
                    title = "Teste Liquidez diária",
                    value = 2000,
                    date_buy = new DateTime(2024, 7, 12).ToUniversalTime(),
                    index = IdexesType.CDI,
                    index_percent = 100m,
                    taxes = true
                },
                new Investment
                {
                    id = SnowflakeGuid.NewGuid(),
                    owner = user,
                    bank = c6,
                    title = "LCI PRE - qualqwuer coisa",
                    value = 1000,
                    date_buy = new DateTime(2024, 12, 20).ToUniversalTime(),
                    due_date = new DateTime(2026, 06, 22).ToUniversalTime(),
                    index = IdexesType.IPCA_MAIS,
                    index_value = 5.9m,
                    taxes = true
                }
            );

            foreach (var item in investments)
            {
                dbContext.Set<Investment>().Add(item);
            }
            dbContext.SaveChanges();
        }
    }
}

static bool IsLoginOrSignupEndpoint(PathString path)
{
    return path.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("/signup", StringComparison.OrdinalIgnoreCase);
}

static bool IsAuthenticatedForRateLimit(HttpContext context)
{
    if (context.User?.Identity?.IsAuthenticated == true) return true;
    if (context.Items.ContainsKey("User")) return true;

    if (!string.IsNullOrWhiteSpace(context.Request.Cookies["jwt"])) return true;

    var authorization = context.Request.Headers.Authorization.ToString();
    return !string.IsNullOrWhiteSpace(authorization) &&
           authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
}

static string GetAuthenticatedPartitionKey(HttpContext context)
{
    string? cookieToken = context.Request.Cookies["jwt"];
    if (!string.IsNullOrWhiteSpace(cookieToken))
        return $"cookie:{cookieToken}";

    string authorization = context.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(authorization) &&
        authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return $"bearer:{authorization["Bearer ".Length..]}";
    }

    string? subject = context.User?.FindFirst("sub")?.Value;
    if (!string.IsNullOrWhiteSpace(subject))
        return $"sub:{subject}";

    return $"ip:{GetIpAddress(context)}";
}

static string GetIpAddress(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
}
