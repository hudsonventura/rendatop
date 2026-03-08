using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using server.BackgroundServices;
using server.Domain;
using server.Middlewares;
using server.Utils;
using StackExchange.Redis;


Env.Load();

//tenta carregar o .env no diretorio pai, se houver
Env.Load("../.env");

SnowflakeGuid.SetMachineID(0);


var builder = WebApplication.CreateBuilder(args);

INotification telegram = new Telegram(Environment.GetEnvironmentVariable("TELEGRAM_TOKEN"), Environment.GetEnvironmentVariable("TELEGRAM_CHATID"));
builder.Services.AddSingleton<INotification>(telegram);
//_notify.Notify("Vencimento", $"Tem investimento vencendo hj carai, que tal resgata-lo? {Environment.NewLine} {investments[0].bank} {Environment.NewLine} {investments[0].title}");


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Starting web application");

builder.Services.AddSerilog();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddHttpContextAccessor(); //usado para injetar o IHttpContextAccessor no construtor dos controllers


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
    config.Title = "Controle de Investimentos";
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

builder.Services.AddScoped<Context>();
MigrateDatabase();

string redis_host = Environment.GetEnvironmentVariable("REDIS_HOST");
string redis_pass = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect($"{redis_host},password={redis_pass},abortConnect=false"));

//Busca os idices do governo
//builder.Services.AddSingleton<Context>();

//serviço que busca as taxas Selic do Banco Central
builder.Services.AddHostedService<SelicBackgroundService>(provider =>
{
    var context = new Context();
    var logger = provider.GetService<ILogger<SelicBackgroundService>>()!;
    return new SelicBackgroundService(logger, context);
});

//serviço que busca as taxas IPCA do Banco Central
builder.Services.AddHostedService<IPCABackgroundService>(provider =>
{
    var context = new Context();
    var logger = provider.GetService<ILogger<IPCABackgroundService>>()!;
    return new IPCABackgroundService(logger, context);
});

//serviço de envio de notificações de vencimento de investimentos
builder.Services.AddHostedService<NotificationBackgroudService>(provider =>
{
    var context = new Context();
    var logger = provider.GetService<ILogger<NotificationBackgroudService>>()!;
    return new NotificationBackgroudService(logger, context, telegram);
});

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
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Ativar autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseAuthenticationMiddleware();



app.Run();



void MigrateDatabase(){
    var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<Context>();
            })
    .Build();
    Log.Information("Applying migrations");
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<Context>();
        // Aplica a migração
        dbContext.Database.Migrate();
        Log.Information("Migrations applied");
        


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

        //adiciona um investimento
        if (!dbContext.Set<Investment>().Any())
        {
            List<Investment> investments = new List<Investment>();
            investments.AddRange(
                new Investment
                {
                    id = SnowflakeGuid.NewGuid(),
                    owner = user,
                    bank = "Sofisa",
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
                    bank = "Sofisa",
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
                    bank = "Banco Inter",
                    title = "Teste Liquidez diária",
                    value = 2000,
                    date_buy = new DateTime(2024, 7, 12).ToUniversalTime(),
                    //date_expected_sell = new DateTime(2026, 06, 22).ToUniversalTime(),
                    index = IdexesType.CDI,
                    index_percent = 100m,
                    taxes = true
                },              new Investment
                {
                    id = SnowflakeGuid.NewGuid(),
                    owner = user,
                    bank = "C6 Bank",
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
                    Code = 876
                },
                new Bank
                {
                    Id = SnowflakeGuid.NewGuid(),
                    Name = "Banco Inter",
                    CompanyName = "Banco Inter S.A.",
                    Cnpj = "977878978678",
                    Code = 876
                },
                new Bank
                {
                    Id = SnowflakeGuid.NewGuid(),
                    Name = "C6 Bank",
                    CompanyName = "C6 Bank S.A.",
                    Cnpj = "977878978678",
                    Code = 876
                }
            );
            foreach (var item in banks)
            {
                dbContext.Set<Bank>().Add(item);
            }
            dbContext.SaveChanges();
        }
    }
}
