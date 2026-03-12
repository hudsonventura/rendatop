using Microsoft.EntityFrameworkCore;


namespace server.Domain;

public class Context : DbContext
{
    public Context(DbContextOptions options) : base(options) {}

 

    public DbSet<User> users { get; set; }
    public DbSet<Selic> selics { get; set; }
    public DbSet<IPCA> ipcas { get; set; }
    public DbSet<Bank> banks { get; set; }

    public DbSet<Investment> investments { get; set; }
    public DbSet<Redemption> redemptions { get; set; }
    public DbSet<Notification> notifications { get; set; }
    

}
