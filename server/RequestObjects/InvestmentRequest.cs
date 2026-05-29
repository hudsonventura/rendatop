

using server.Domain;
using System.Text.Json.Serialization;

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
    /// Tipo do investimento. Opcional.
    /// </summary>
    public InvestmentType? investment_type { get; set; }

    /// <summary>
    /// Cofrinho do investimento. Opcional.
    /// </summary>
    public Guid? money_box_id { get; set; }

    /// <summary>
    /// Carteira do investimento.
    /// </summary>
    public Guid? wallet_id { get; set; }

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
    /// Alias usado pelo frontend para data de vencimento
    /// </summary>
    [JsonPropertyName("due_date")]
    public DateTime? due_date
    {
        get => date_expected_sell;
        set => date_expected_sell = value;
    }

    /// <summary>
    /// Valor do investido
    /// </summary>
    public decimal value { get; set; }

    /// <summary>
    /// Indice associado ao investimento. Ex.: %a.a., CDI, IPCA, IPCA + x%, etc.
    /// </summary>
    public IdexesType index { get; set; }

    /// <summary>
    /// Percentual do índice. Ex.: X% ao ano, X% do CDI ou IPCA+X%
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
    /// Indica se o investimento está arquivado.
    /// </summary>
    public bool archived { get; set; } = false;

    /// <summary>
    /// Indica se o preenchimento inicial deste investimento usou a leitura automática de comprovantes.
    /// Campo transitório para contabilização de uso no momento do salvamento.
    /// </summary>
    public bool ai_extracted { get; set; } = false;

}
