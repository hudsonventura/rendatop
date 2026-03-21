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
    public int ai_monthly_limit { get; set; }

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
            features = new Dictionary<string, string>
            {
                { "ai_usage", "2 consultas de IA por mês" },
                { "investments", "Controle básico de investimentos" },
                { "notifications", "Notificações de vencimento" },
                { "calendar", "Calendário de vencimentos" }
            }
        },
        new Plan
        {
            id = "plus",
            name = "Plus",
            price = 9.90m,
            ai_monthly_limit = 10,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "10 consultas de IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "notifications", "Notificações de vencimento" },
                { "calendar", "Calendário de vencimentos" },
                { "export_data", "Exportação de dados (em breve)" },
                { "priority_support", "Suporte prioritário" }
            }
        },
        new Plan
        {
            id = "pro",
            name = "Pro",
            price = 16.90m,
            ai_monthly_limit = 30,
            features = new Dictionary<string, string>
            {
                { "ai_usage", "30 consultas de IA por mês" },
                { "investments", "Controle completo de investimentos" },
                { "notifications", "Notificações de vencimento" },
                { "calendar", "Calendário de vencimentos" },
                { "export_data", "Exportação de dados (em breve)" },
                { "import_data", "Importação de dados (em breve)" },
                { "priority_support", "Suporte prioritário" },
                { "unlimited_investments", "Investimentos ilimitados (em breve)" }
            }
        }
    };

    public static Plan? GetById(string id) => All.FirstOrDefault(p => p.id == id);
}
