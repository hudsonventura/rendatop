using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using server.Domain;

try
{
    // Keep behavior close to backend startup.
    Env.Load();
    Env.Load("../.env");

    Console.WriteLine("Starting migrations applier...");

    var factory = new ContextFactory();
    
    await using var db = factory.CreateDbContext(args);

    Console.WriteLine("Applying EF Core migrations...");
    await db.Database.MigrateAsync();

    Console.WriteLine("Migrations applied successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Migration failed.");
    Console.Error.WriteLine(ex.ToString());
    return 1;
}
