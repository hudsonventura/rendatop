using api.domain;
using api.middlewares;
using api.repositories;
using api.services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;


Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "rendatop",
            ValidAudience = "rendatop",
            IssuerSigningKey = JwtService.GetSecurityKey(),
            NameClaimType = "name",
        };

        // Ler o token do cookie "session" ao invés do header Authorization
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookie = context.Request.Cookies["session"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    context.Token = cookie;
                }
                return Task.CompletedTask;
            }
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                Environment.GetEnvironmentVariable("CORS_ORIGIN") ?? "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

Result go_on = DepenciesInjection();



var app = builder.Build();




app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseJwtMiddleware();

app.MapControllers();


if (app.Environment.IsDevelopment())
{
    Thread.Sleep(TimeSpan.FromSeconds(10));
    AutoMigration();
    await PopulateFakeDatabase();

    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (go_on.IsFailure)
{
    Console.WriteLine(go_on.Message);
    return;
}

app.Run();


void AutoMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<DBContext>();
        context.Database.Migrate();
    }
}

Result DepenciesInjection()
{

    string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? string.Empty;
    string port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? string.Empty;
    string db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? string.Empty;
    string user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? string.Empty;
    string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? string.Empty;

    string connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password}";


    Console.WriteLine(connectionString);
    builder.Services.AddDbContext<DBContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });
    if (string.IsNullOrEmpty(host))
        return Result.Failure($"Is the .env file in the correct place? I cannot read POSTGRES_HOST. I'm looking at {Environment.CurrentDirectory}");


    builder.Services.AddScoped<UserRepository>();
    return Result.Success();
}






async Task PopulateFakeDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var user_repo = services.GetRequiredService<UserRepository>();
        string salt = Guid.NewGuid().ToString();
        await user_repo.Create(new User
        {
            Name = "Hudson Ventura",
            Email = "teste@teste.com",
            Password = Encrypt.HashPassword("123456", salt),
            Salt = salt
        });
    }
}