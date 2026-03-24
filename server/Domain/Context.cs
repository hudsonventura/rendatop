using Microsoft.EntityFrameworkCore;


namespace server.Domain;

/// <summary>
/// Contexto de banco de dados
/// </summary>
public class Context : DbContext
{
    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="options"></param>
    public Context(DbContextOptions options) : base(options) {}

 
    /// <summary>
    /// Tabela de usuarios do sistema
    /// </summary>
    public DbSet<User> users { get; set; }

    /// <summary>
    /// Tabela de selic
    /// </summary>
    public DbSet<Selic> selics { get; set; }

    /// <summary>
    /// Tabela de IPCA
    /// </summary>
    public DbSet<IPCA> ipcas { get; set; }

    /// <summary>
    /// Tabela de bancos
    /// </summary>
    public DbSet<Bank> banks { get; set; }

    /// <summary>
    /// Tabela de investimentos
    /// </summary>
    public DbSet<Investment> investments { get; set; }


    /// <summary>
    /// Tabela de resgates
    /// </summary>
    public DbSet<Redemption> redemptions { get; set; }


    /// <summary>
    /// Tabela de notificação
    /// </summary>
    public DbSet<Notification> notifications { get; set; }

    /// <summary>
    /// Tabela de assinaturas
    /// </summary>
    public DbSet<Subscription> subscriptions { get; set; }
    

}
