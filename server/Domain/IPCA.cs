
using System.ComponentModel.DataAnnotations;

namespace server.Domain;

public class IPCA
{
    /// <summary>
    /// Data da taxa IPCA - 1 IPCA por mes sendo dia 01 de cada mês
    /// </summary>
    [Key]
    public DateOnly date { get; set; }

    /// <summary>
    /// Valor do IPCA do mês
    /// </summary>
    public decimal value { get; set; }
}
