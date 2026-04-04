using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace server.Domain;

public class ContextFactory : IDesignTimeDbContextFactory<Context>
{
    public Context CreateDbContext(string[] args)
    {
        DotNetEnv.Env.Load();
        
        string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        string port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        string db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "postgres";
        string user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "env not loaded";
        string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "env not loaded";

        string connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password}";

        var optionsBuilder = new DbContextOptionsBuilder<Context>();
        optionsBuilder.UseNpgsql(connectionString);
        return new Context(optionsBuilder.Options);
    }
}
