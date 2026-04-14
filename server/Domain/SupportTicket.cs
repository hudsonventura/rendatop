using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Domain;

public enum SupportTicketStatus
{
    AguardandoAtendimento,
    EmAtendimento,
    AguardandoRespostaUsuario,
    Encerrado,
    Cancelado
}

public enum SupportTicketChangeSource
{
    SystemOnCreate,
    AdminManual,
    SystemOnUserReply
}

[Table("support_tickets")]
public class SupportTicket
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("requester_user_id")]
    [JsonIgnore]
    public User requester_user { get; set; } = null!;
    public Guid requester_user_id { get; set; }

    public string requester_user_name { get; set; } = string.Empty;
    public string requester_user_email { get; set; } = string.Empty;
    public string subject { get; set; } = string.Empty;
    public SupportTicketStatus status { get; set; } = SupportTicketStatus.AguardandoAtendimento;
    public DateTime last_message_at { get; set; } = DateTime.UtcNow;
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
    public DateTime? archived_at { get; set; }

    [JsonIgnore]
    public ICollection<SupportTicketMessage>? messages { get; set; }

    [JsonIgnore]
    public ICollection<SupportTicketStatusHistory>? status_history { get; set; }
}

[Table("support_ticket_messages")]
public class SupportTicketMessage
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("ticket_id")]
    [JsonIgnore]
    public SupportTicket ticket { get; set; } = null!;
    public Guid ticket_id { get; set; }

    [ForeignKey("sender_user_id")]
    [JsonIgnore]
    public User sender_user { get; set; } = null!;
    public Guid sender_user_id { get; set; }

    public UserType sender_user_type { get; set; }
    public string sender_user_name { get; set; } = string.Empty;
    public string body_html { get; set; } = string.Empty;
    public string body_text { get; set; } = string.Empty;
    public DateTime created_at { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<SupportTicketMessageAttachment>? attachments { get; set; }
}

[Table("support_ticket_message_attachments")]
public class SupportTicketMessageAttachment
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("message_id")]
    [JsonIgnore]
    public SupportTicketMessage message { get; set; } = null!;
    public Guid message_id { get; set; }

    public string file_name { get; set; } = string.Empty;
    public string content_type { get; set; } = string.Empty;
    public long size_bytes { get; set; }
    public bool is_image { get; set; }
    public byte[] content { get; set; } = [];
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}

[Table("support_ticket_status_history")]
public class SupportTicketStatusHistory
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("ticket_id")]
    [JsonIgnore]
    public SupportTicket ticket { get; set; } = null!;
    public Guid ticket_id { get; set; }

    [ForeignKey("actor_user_id")]
    [JsonIgnore]
    public User actor_user { get; set; } = null!;
    public Guid actor_user_id { get; set; }

    public string actor_user_name { get; set; } = string.Empty;
    public SupportTicketStatus? from_status { get; set; }
    public SupportTicketStatus to_status { get; set; }
    public SupportTicketChangeSource source { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}
