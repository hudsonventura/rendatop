

using server.Domain;

namespace server.RequestObjects;

/// <summary>
/// Objeto do tipo investimento renda fixa (CDB, LCI/LCA, etc)
/// </summary>
public class InvestmentRequest
{
    /// <summary>
    /// Uma identificação sobre o investimento
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// Código do banco onde o investimento foi feito
    /// </summary>
    public int bank_code { get; set; }


    /// <summary>
    /// Data de compra
    /// </summary>
    public DateTime date_buy { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Data esperada da venda/liquidação em conta. Pode ser vazia
    /// </summary>
    public DateTime? date_expected_sell { get; set; }

    /// <summary>
    /// Valor do investido
    /// </summary>
    public decimal value { get; set; }

    /// <summary>
    /// Indice associado ao investimento. Ex.: %a.a., CDI, IPCA, IPCA + x%, etc.
    /// </summary>
    public IdexesType index { get; set; }

    /// <summary>
    /// Percentual do índice. Ex.: 12% ao ano, 110% do CDI ou 96% do IPCA
    /// </summary>
    public decimal index_percent { get; set; }

    /// <summary>
    /// Valor excedente a ser somado. Usar em casos, por exemplo de IPCA + 5%
    /// </summary>
    public decimal index_value { get; set; }

    /// <summary>
    /// Indica se o investimento possui ou não a incidência de impostos. Os impostos são calculados com base na tabela de IR
    /// </summary>
    public bool taxes { get; set; } = true;


    /// <summary>
    /// Indica o tipo de calculo a ser feito. Se true, calcula os valores atuais, se false, calcula os valores futuros, considerando o vencimento
    /// </summary>
    public bool atual { get; set; }
}
