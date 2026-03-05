using System.ComponentModel.DataAnnotations;

namespace server.Domain;

public class Selic
{
    /// <summary>
    /// Data da taxa Selic
    /// </summary>
    [Key]
    //[DisplayFormat(DataFormatString = "{0:dd/MM/YYYY}", ApplyFormatInEditMode = true)]
    //[Display(Name = "Data da Selic")]
    public DateOnly date { get; set; }

    /// <summary>
    /// Valor da Selic na data
    /// </summary>
    public decimal value { get; set; }
}
