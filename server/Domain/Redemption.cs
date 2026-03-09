using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using server.RequestObjects;


namespace server.Domain;

/// <summary>
/// Resgate de um investimento
/// </summary>
public class Redemption
{
    /// <summary>
    /// Chave primaria
    /// </summary>
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    /// <summary>
    /// O investimento a ser resgatado
    /// </summary>
    [JsonIgnore]
    public Investment investment { get; set; }


    /// <summary>
    /// Uma identificação sobre o resgate
    /// </summary>
    public string title { get; set; }


    /// <summary>
    /// Data do resgate
    /// </summary>
    public DateTime date { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Valor do Resgate
    /// </summary>
    public decimal value { get; set; }


    public Redemption(){}
    
    public Redemption(Investment invest, RedemptionRequest request)
    {
        investment = invest;
        title = request.title;
        value = request.value;
        date = request.date;
    }
}
