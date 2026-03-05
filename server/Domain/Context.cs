using Microsoft.EntityFrameworkCore;


namespace server.Domain;

public class Context : DbContext
{
    private string _stringConnection = $@"NOT SET YET";
    public Context() : base(){
        string Host = Environment.GetEnvironmentVariable("DB_HOST");
        string Username = Environment.GetEnvironmentVariable("DB_USER");
        string Password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        string Database = Environment.GetEnvironmentVariable("DB_NAME");
        _stringConnection = $"Host={Host};Username={Username};Password={Password};Database={Database}";
    }

    
 
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(_stringConnection)
                        //.EnableSensitiveDataLogging()
					    //.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
                        ;
  

    public DbSet<User> users { get; set; }
    public DbSet<Selic> selics { get; set; }
    public DbSet<IPCA> ipcas { get; set; }


    public DbSet<Investment> investments { get; set; }
    public DbSet<Redemption> redemptions { get; set; }
    

}
