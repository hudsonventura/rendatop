using back;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;


Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

DepenciesInjection();


var app = builder.Build();

AutoMigration();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


void DepenciesInjection()
{

    builder.Services.AddDbContext<DBContext>(options =>
    {
        string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? string.Empty;
        string port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? string.Empty;
        string db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? string.Empty;
        string user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? string.Empty;
        string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? string.Empty;

        string connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password}";

        if (string.IsNullOrEmpty(host))
            throw new Exception($"Is the .env file in the correct place? I cannot read POSTGRES_HOST. I'm looking at {Environment.CurrentDirectory}");

        Console.WriteLine(connectionString);
        options.UseNpgsql(connectionString);
    });
}



void AutoMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<DBContext>();
        context.Database.Migrate();
    }
}