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

        modelBuilder.Entity<User>()
            .Property(user => user.user_type)
            .HasDefaultValue(UserType.Common);

        modelBuilder.Entity<User>()
            .Property(user => user.auth_provider)
            .HasDefaultValue(AuthProvider.Password);

        modelBuilder.Entity<SupportTicket>()
            .Property(ticket => ticket.status)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicket>()
            .HasIndex(ticket => new { ticket.status, ticket.last_message_at });

        modelBuilder.Entity<SupportTicket>()
            .HasIndex(ticket => new { ticket.requester_user_id, ticket.status });

        modelBuilder.Entity<SupportTicket>()
            .HasIndex(ticket => new { ticket.archived_at, ticket.last_message_at });

        modelBuilder.Entity<SupportTicketMessage>()
            .Property(message => message.sender_user_type)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicketMessage>()
            .HasIndex(message => new { message.ticket_id, message.created_at });

        modelBuilder.Entity<SupportTicketMessageAttachment>()
            .HasIndex(attachment => attachment.message_id);

        modelBuilder.Entity<SupportTicketStatusHistory>()
            .Property(history => history.from_status)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicketStatusHistory>()
            .Property(history => history.to_status)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicketStatusHistory>()
            .Property(history => history.source)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicketStatusHistory>()
            .HasIndex(history => new { history.ticket_id, history.created_at });

        modelBuilder.Entity<BlogPost>()
            .Property(post => post.status)
            .HasConversion<string>();

        modelBuilder.Entity<BlogPost>()
            .HasIndex(post => post.slug)
            .IsUnique();

        modelBuilder.Entity<BlogPost>()
            .HasIndex(post => new { post.status, post.published_at });

        modelBuilder.Entity<BlogPost>()
            .HasIndex(post => post.updated_at);

        modelBuilder.Entity<BlogPostAsset>()
            .HasIndex(asset => asset.blog_post_id);

        modelBuilder.Entity<BlogPostSocialPublication>()
            .Property(publication => publication.channel)
            .HasConversion<string>();

        modelBuilder.Entity<BlogPostSocialPublication>()
            .Property(publication => publication.status)
            .HasConversion<string>();

        modelBuilder.Entity<BlogPostSocialPublication>()
            .HasIndex(publication => new { publication.blog_post_id, publication.channel })
            .IsUnique();

        modelBuilder.Entity<AiUsage>()
            .HasIndex(x => new { x.user_id, x.feature, x.created_at });

        modelBuilder.Entity<BrowserPushSubscription>()
            .HasIndex(x => x.user_id);

        modelBuilder.Entity<BrowserPushSubscription>()
            .HasIndex(x => x.endpoint)
            .IsUnique();
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
    /// Tabela de inscrições Web Push para notificações no navegador
    /// </summary>
    public DbSet<BrowserPushSubscription> browser_push_subscriptions { get; set; }

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

    /// <summary>
    /// Tabela de chamados de atendimento.
    /// </summary>
    public DbSet<SupportTicket> support_tickets { get; set; }

    /// <summary>
    /// Tabela de mensagens dos chamados.
    /// </summary>
    public DbSet<SupportTicketMessage> support_ticket_messages { get; set; }

    /// <summary>
    /// Tabela de anexos das mensagens dos chamados.
    /// </summary>
    public DbSet<SupportTicketMessageAttachment> support_ticket_message_attachments { get; set; }

    /// <summary>
    /// Tabela de histórico de status dos chamados.
    /// </summary>
    public DbSet<SupportTicketStatusHistory> support_ticket_status_history { get; set; }

    /// <summary>
    /// Tabela de postagens do blog.
    /// </summary>
    public DbSet<BlogPost> blog_posts { get; set; }

    /// <summary>
    /// Tabela de assets de imagem do blog.
    /// </summary>
    public DbSet<BlogPostAsset> blog_post_assets { get; set; }

    /// <summary>
    /// Tabela de status de publicações sociais do blog.
    /// </summary>
    public DbSet<BlogPostSocialPublication> blog_post_social_publications { get; set; }

}
