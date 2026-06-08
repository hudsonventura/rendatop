namespace server.Domain;

/// <summary>
/// Definição centralizada de todos os planos de assinatura.
/// Altere apenas este arquivo para gerenciar planos.
/// </summary>
public class Plan
{
    

    internal string description { get; set; } = string.Empty;

    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    
    /// <summary>
    /// Valor do plano em reais. Ex: 4.9 para R$4,90.
    /// </summary>
    public decimal price { get; set; } = 0;

    /// <summary>
    /// Limite de investimentos em renda fixa para o plano.
    /// </summary>
    internal int investments { get; set; } = 15;

    /// <summary>
    /// Limite de leituras de comprovantes por mês
    /// </summary>
    public int ai_monthly_limit { get; set; } = 0;

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
    internal int money_boxes_limit { get; set; } = 2;

    /// <summary>
    /// Limite de carteiras disponíveis no plano.
    /// </summary>
    internal int wallets_limit { get; set; } = 1;

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
            description = "Plano gratuito para controle financeiro básico",
            price = 0m,
            ai_monthly_limit = 0,
            calendar_ics = false,
            investments = 15,
            stoks = 3,
            whatsapp_notifications = false,
            recurring_investments = false,
            money_boxes_limit = 2,
            wallets_limit = 1,
            features = new Dictionary<string, string>
            {
                { "investments", "Controle de até 15 investimentos em renda fixa" },
                { "wallets", "1 carteira" },
                { "money_boxes", "Até 2 cofrinhos para organizar sua carteira" },
                { "calendar_ics", "Calendário de vencimentos no app" },
                { "notifications", "Notificações de vencimento (Telegram e E-mail)" },
                { "stoks", "Controle de ações (em breve)" },
                { "priority_support", "Suporte padrão" },
            }
        },
        new Plan
        {
            id = "starter",
            name = "Starter",
            description = "Plano com mais limites para usuários que possuem mais investimentos",
            price = 4.9m,
            ai_monthly_limit = 2,
            calendar_ics = false,
            stoks = 10,
            investments = 30,
            whatsapp_notifications = false,
            recurring_investments = true,
            money_boxes_limit = 5,
            wallets_limit = 5,
            features = new Dictionary<string, string>
            {  
                { "investments", "Controle de até 30 investimentos em renda fixa" },
                { "wallets", "1 carteira" },
                { "money_boxes", "Até 5 cofrinhos para organizar sua carteira" },
                { "calendar_ics", "Calendário de vencimentos no app" },
                { "notifications", "Notificações de vencimento (Telegram e E-mail)" },
                { "stoks", "Controle de ações (em breve)" },
                { "priority_support", "Suporte prioritário" },

                { "ai_usage", "2 leituras automáticas de comprovantes por mês" },

            }
        },
        new Plan
        {
            id = "plus",
            name = "Plus",
            description = "Plano intermediário para usuários que desejam mais recursos",
            price = 8.9m,
            ai_monthly_limit = 10,
            calendar_ics = true,
            stoks = 30,
            investments = 50,
            whatsapp_notifications = true,
            recurring_investments = true,
            money_boxes_limit = int.MaxValue,
            wallets_limit = 5,
            features = new Dictionary<string, string>
            {
                { "investments", "Controle de até 50 investimentos em renda fixa" },
                { "wallets", "Até 5 carteiras" },
                { "money_boxes", "Cofrinhos ilimitados" },
                { "calendar_ics", "Calendário de vencimentos no seu Outlook ou App de calendário" },
                { "whatsapp_notifications", "Notificações de vencimento (Telegram, E-mail e WhatsApp)" },
                { "stoks", "Controle de ações (em breve)" },
                { "priority_support", "Suporte prioritário" },

                { "ai_usage", "10 leituras automáticas de comprovantes por mês" },
                { "recurring_investments", "Investimentos recorrentes automatizados" },
                { "pripto", "Controle de criptomoedas (em breve)" },
                { "export_export_data", "Importação e Exportação de dados (em breve)" },
            }
        },
        new Plan
        {
            id = "pro",
            name = "Pro",
            description = "Plano profissional para usuários que desejam mais limites",
            price = 14.9m,
            ai_monthly_limit = 30,
            calendar_ics = true,
            stoks = 100,
            investments = int.MaxValue,
            whatsapp_notifications = true,
            recurring_investments = true,
            money_boxes_limit = int.MaxValue,
            wallets_limit = int.MaxValue,
            features = new Dictionary<string, string>
            {
                { "investments", "Controle ilimitado de investimentos" },
                { "wallets", "Carteiras ilimitadas" },
                { "money_boxes", "Cofrinhos ilimitados" },
                { "calendar_ics", "Calendário de vencimentos no seu Outlook ou App de calendário" },
                { "whatsapp_notifications", "Notificações de vencimento (Telegram, E-mail e WhatsApp)" },
                { "stoks", "Controle de ações (em breve)" },
                { "priority_support", "Suporte prioritário" },

                { "ai_usage", "30 leituras automáticas de comprovantes por mês" },
                { "recurring_investments", "Investimentos recorrentes automatizados" },
                { "pripto", "Controle de criptomoedas (em breve)" },
                { "export_export_data", "Importação e Exportação de dados (em breve)" },
            }
        }
    };

    public static Plan? GetById(string id) => All.FirstOrDefault(p => p.id == id);
}
