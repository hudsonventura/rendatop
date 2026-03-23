namespace server.Domain;

/// <summary>
/// Definição centralizada de todos os planos de assinatura.
/// Altere apenas este arquivo para gerenciar planos.
/// </summary>
public class Plan
{
    

    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public decimal price { get; set; }

    /// <summary>
    /// Limite de leituras de comprovantes por IA por mês
    /// </summary>
    public int ai_monthly_limit { get; set; } = 2;

    /// <summary>
    /// Usuario tem permissao para usar o ICS?
    /// </summary>
    internal bool calendar_ics { get; set; } = false;

    /// <summary>
    /// Quantidade de stoks para o plano
    /// </summary>
    internal int stoks { get; set; } = 0;

    /// <summary>
    /// Usuario tem permissao para usar notificação de Whatsapp?
    /// </summary>
    internal bool whatsapp_notifications { get; set; } = false;

    /// <summary>
    /// Dicionário de features do plano.
    /// Key = identificador da feature (ex: "ai_usage", "export_data").
    /// Value = texto explicativo para exibição ao usuário.
    /// </summary>
    public Dictionary<string, string> features { get; set; } = new();
}


public static class Plans
{

    public static readonly List<Plan> All = new()
    {
        new Plan
        {
            id = "free",
            name = "Free",
            price = 0m,
            ai_monthly_limit = 2,
            calendar_ics = false,
            stoks = 5,
            whatsapp_notifications = false,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "2 leituras de comprovantes por IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "calendar_ics", "Calendário de vencimentos" },
                { "notifications", "Notificações de vencimento (Telegram e E-mail)" },
                {"stoks", "Controle de ações brasileiras, até 5 posições em aberto (em breve)" },
            }
        },
        new Plan
        {
            id = "plus",
            name = "Plus",
            price = 9.90m,
            ai_monthly_limit = 10,
            calendar_ics = true,
            stoks = 30,
            whatsapp_notifications = true,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "10 leituras de comprovantes por IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "calendar_ics", "Calendário de vencimentos no seu Outlook ou App de calendário" },
                { "notifications", "Notificações de vencimento (Telegram e E-mail)" },
                { "whatsapp_notifications", "Notificações de vencimento via WhatsApp" },
                { "export_export_data", "Importação e Exportação de dados (em breve)" },
                { "priority_support", "Suporte prioritário" },
                { "stoks", "Controle de ações brasileiras, até 30 ativos (em breve)" },
            }
        },
        new Plan
        {
            id = "pro",
            name = "Pro",
            price = 16.90m,
            ai_monthly_limit = 30,
            calendar_ics = true,
            stoks = 100,
            whatsapp_notifications = true,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "10 leituras de comprovantes por IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "calendar_ics", "Calendário de vencimentos no seu Outlook ou App de calendário" },
                { "notifications", "Notificações de vencimento (Telegram e E-mail)" },
                { "whatsapp_notifications", "Notificações de vencimento via WhatsApp" },
                { "export_export_data", "Importação e Exportação de dados (em breve)" },
                { "priority_support", "Suporte prioritário" },
                { "stoks", "Controle de ações brasileiras, até 100 ativos (em breve)" },
            }
        }
    };

    public static Plan? GetById(string id) => All.FirstOrDefault(p => p.id == id);
}
