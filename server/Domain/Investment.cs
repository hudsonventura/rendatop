using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using server.Domain;
using server.RequestObjects;

namespace server.Domain;

/// <summary>
/// Objeto do tipo investimento renda fixa (CDB, LCI/LCA, etc)
/// </summary>
public class Investment
{
    /// <summary>
    /// Chave primaria
    /// </summary>
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    /// <summary>
    /// Dono do investimento
    /// </summary>
    public User owner { get; set; }

    /// <summary>
    /// Uma identificação sobre o investimento
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// String que represta o banco onde o investimento foi feito
    /// </summary>
    public string bank { get; set; }

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
    /// Valor excedente a ser somado. Usar em casos, por exemplo de IPCA + 5%. Default 0
    /// </summary>
    public decimal index_value { get; set; } = 0;

    /// <summary>
    /// Indica se o investimento possui ou não a incidência de impostos. Os impostos são calculados com base na tabela de IR
    /// </summary>
    public bool taxes { get; set; }


    [NotMapped] //impede que vá para o entity
    public List<Calculated> calculated { get; set; }



    public Investment(){}

    public Investment(InvestmentRequest request, User owner)
    {
        this.owner = owner;
        this.title = request.title;
        this.bank = request.bank;
        this.value = request.value;
        this.index = request.index;
        this.index_percent = request.index_percent;
        this.index_value = request.index_value;
        this.taxes = request.taxes;
        this.date_buy = DateTime.SpecifyKind(request.date_buy, DateTimeKind.Utc);
        this.date_expected_sell = request.date_expected_sell is null ? null : DateTime.SpecifyKind((DateTime) request.date_expected_sell, DateTimeKind.Utc);
    }

    public void Update(InvestmentRequest request)
    {
        this.owner = owner;
        this.title = request.title;
        this.bank = request.bank;
        this.value = request.value;
        this.index = request.index;
        this.index_percent = request.index_percent;
        this.index_value = request.index_value;
        this.taxes = request.taxes;
    }

    internal InvestmentRequest ToRequest()
    {
        return new InvestmentRequest(){
            title = this.title,
            bank = this.bank,
            value = this.value,
            index = this.index,
            index_percent = this.index_percent,
            index_value = this.index_value,
            taxes = this.taxes,
            date_buy = this.date_buy,
            date_expected_sell = this.date_expected_sell
        };
    }
}
