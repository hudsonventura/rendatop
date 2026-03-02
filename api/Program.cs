using api.repositories;
using api.services;
using back;
using back.domain;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;


Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    Thread.Sleep(TimeSpan.FromSeconds(10));
    AutoMigration();
    await PopulateFakeDatabase();
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

        await user_repo.Create(new User
        {
            Name = "Hudson Ventura",
            Email = "teste@teste.com",
            Password = Encrypt.HashPassword("123456", Guid.NewGuid().ToString()),
            Salt = Guid.NewGuid().ToString()
        });
    }
}