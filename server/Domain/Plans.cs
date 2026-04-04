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
    /// Usuario tem permissao para criar investimentos recorrentes?
    /// </summary>
    internal bool recurring_investments { get; set; } = false;

    /// <summary>
    /// Limite de cofrinhos disponíveis no plano.
    /// </summary>
    internal int money_boxes_limit { get; set; } = 3;

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
            recurring_investments = false,
            money_boxes_limit = 3,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "2 leituras de comprovantes por IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "money_boxes", "Até 3 cofrinhos para organizar sua carteira" },
                { "calendar_ics", "Calendário de vencimentos" },
                { "notifications", "Notificações de vencimento (Telegram e E-mail)" },
                {"stoks", "Controle de ações brasileiras, até 5 posições em aberto (em breve)" },
                { "priority_support", "Suporte padrão" },
            }
        },
        new Plan
        {
            id = "plus",
            name = "Plus",
            price = 6.9m,
            ai_monthly_limit = 10,
            calendar_ics = true,
            stoks = 30,
            whatsapp_notifications = true,
            recurring_investments = true,
            money_boxes_limit = int.MaxValue,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "10 leituras de comprovantes por IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "money_boxes", "Cofrinhos ilimitados" },
                { "calendar_ics", "Calendário de vencimentos no seu Outlook ou App de calendário" },
                { "whatsapp_notifications", "Notificações de vencimento (Telegram, E-mail e WhatsApp)" },
                { "export_export_data", "Importação e Exportação de dados (em breve)" },
                { "stoks", "Controle de ações brasileiras, até 30 posições em aberto (em breve)" },
                { "pripto", "Controle de criptomoedas, até 10 posições em aberto (em breve)" },
                { "priority_support", "Suporte prioritário" },
            }
        },
        new Plan
        {
            id = "pro",
            name = "Pro",
            price = 14.9m,
            ai_monthly_limit = 30,
            calendar_ics = true,
            stoks = 100,
            whatsapp_notifications = true,
            recurring_investments = true,
            money_boxes_limit = int.MaxValue,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "30 leituras de comprovantes por IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "money_boxes", "Cofrinhos ilimitados" },
                { "calendar_ics", "Calendário de vencimentos no seu Outlook ou App de calendário" },
                { "whatsapp_notifications", "Notificações de vencimento (Telegram, E-mail e WhatsApp)" },
                { "export_export_data", "Importação e Exportação de dados (em breve)" },
                { "stoks", "Controle de ações brasileiras, até 100 posições em aberto (em breve)" },
                { "pripto", "Controle de criptomoedas, até 20 posições em aberto (em breve)" },
                { "priority_support", "Suporte prioritário" },
            }
        }
    };

    public static Plan? GetById(string id) => All.FirstOrDefault(p => p.id == id);
}
