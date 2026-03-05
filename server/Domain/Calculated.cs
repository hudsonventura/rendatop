using System.Text.Json.Serialization;

namespace server.Domain;

public record Calculated
{

    #region VALORES BRUTOS

    /// <summary>
    /// Taxa de percentual efetivo do indice, baseado na quantidade de dias de inicio e fim do investimento, sem os impostos
    /// </summary>
    public decimal effective_index_percent_brute { get; set; }

    /// <summary>
    /// Lucro bruto, ainda sem aplicação de impostos
    /// </summary>
    public decimal profit_brute { get; set; }

    /// <summary>
    /// Valor bruto, sem descontos de impostos
    /// </summary>
    public decimal value_brute { get; set; }

    #endregion
    













    #region Imposts
    
    /// <summary>
    /// Percentual do IOF
    /// </summary>\
    [JsonPropertyName("IOF")]
    public decimal IOF { get; internal set; }


    /// <summary>
    /// Valor monetário do IOF
    /// </summary>
    [JsonPropertyName("IOF_value")]
    public decimal IOF_value { get; internal set; }

    /// <summary>
    /// Percentual do Imposto de Renda sobre a transação
    /// </summary>
    [JsonPropertyName("IR")]
    public decimal IR { get; internal set; }

    /// <summary>
    /// Valor monetário do imposto de renda
    /// </summary>
    [JsonPropertyName("IR_value")]
    public decimal IR_value { get; internal set; }
    




    #endregion
    
    











    #region VALORES LIQUIDOS



    /// <summary>
    /// Lucro líquido, já com descontos de impostos
    /// </summary>
    public decimal profit_liq { get; set; }

    /// <summary>
    /// Valor líquido a ser depositado em conta corrente no momento do resgate
    /// </summary>
    public  decimal value_liq { get; set; }
    

    #endregion

}
