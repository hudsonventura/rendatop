using Microsoft.EntityFrameworkCore;
using server.Utils;


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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SubscriptionCharge>()
            .Property(charge => charge.status)
            .HasConversion<string>();

        modelBuilder.Entity<SubscriptionCharge>()
            .Property(charge => charge.charge_kind)
            .HasConversion<string>();

        modelBuilder.Entity<LandingVisit>()
            .HasIndex(x => x.visit);

        modelBuilder.Entity<LandingVisit>()
            .HasIndex(x => x.created_at);

        modelBuilder.Entity<AiUsage>()
            .HasIndex(x => new { x.user_id, x.feature, x.created_at });
    }

    public override int SaveChanges()
    {
        NormalizeDateTimesToUtc();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries().Where(x =>
                     x.State == EntityState.Added ||
                     x.State == EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                var clrType = property.Metadata.ClrType;

                if (clrType == typeof(DateTime) && property.CurrentValue is DateTime value)
                {
                    property.CurrentValue = UtcDateTime.EnsureUtc(value);
                }
                else if (clrType == typeof(DateTime?) && property.CurrentValue is DateTime nullableValue)
                {
                    property.CurrentValue = UtcDateTime.EnsureUtc(nullableValue);
                }
            }
        }
    }

 
    /// <summary>
    /// Tabela de consumo de recursos de IA
    /// </summary>
    public DbSet<AiUsage> ai_usages { get; set; }

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
    /// Tabela de cofrinhos
    /// </summary>
    public DbSet<MoneyBox> money_boxes { get; set; }

    /// <summary>
    /// Tabela de investimentos recorrentes
    /// </summary>
    public DbSet<RecurringInvestment> recurring_investments { get; set; }


    /// <summary>
    /// Tabela de resgates
    /// </summary>
    public DbSet<Redemption> redemptions { get; set; }


    /// <summary>
    /// Tabela de visitas da landing page
    /// </summary>
    public DbSet<LandingVisit> landing_visits { get; set; }

    /// <summary>
    /// Tabela de notificação
    /// </summary>
    public DbSet<Notification> notifications { get; set; }

    /// <summary>
    /// Tabela de assinaturas
    /// </summary>
    public DbSet<Subscription> subscriptions { get; set; }

    /// <summary>
    /// Tabela de cobranças de assinatura
    /// </summary>
    public DbSet<SubscriptionCharge> subscription_charges { get; set; }
    

}
