namespace server.RequestObjects;

/// <summary>
/// Objeto usado para solicitações de resgate de investimentos
/// </summary>
public class RedemptionRequest
{
    /// <summary>
    /// Uma identificação sobre o resgate
    /// </summary>
    public string title { get; set; }


    /// <summary>
    /// Data do resgate
    /// </summary>
    public DateTime date { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Valor do investido
    /// </summary>
    public decimal value { get; set; }
}
