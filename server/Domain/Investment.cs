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
    /// Tipo categórico do investimento. Opcional.
    /// </summary>
    public InvestmentType? investment_type { get; set; }

    /// <summary>
    /// String que represta o banco onde o investimento foi feito
    /// </summary>
    [ForeignKey("bank")]
    public Guid bank_id { get; set; }
    public Bank bank { get; set; }

    /// <summary>
    /// Data de compra
    /// </summary>
    public DateTime date_buy { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Data esperada da venda/liquidação em conta. Pode ser vazia
    /// </summary>
    public DateTime? due_date { get; set; }

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

    /// <summary>
    /// Lista de resgates realizados neste investimento
    /// </summary>
    public List<Redemption> redemptions { get; set; } = new();

    /// <summary>
    /// Indica se o investimento foi arquivado pelo usuário.
    /// </summary>
    public bool archived { get; set; } = false;

    
    [NotMapped] //impede que vá para o entity
    public List<Calculated> calculated { get; set; }

    /// <summary>
    /// Valor total considerando valores resgatados.
    /// </summary>
    [NotMapped]
    public decimal? table_value {
        get
        {
            return _table_value;
        }
        set
        {
            //Se o valor final, considerando os resgates for menor ou igual a zero, então o investimento deve ser arquivado
            if (value <= 0.01m)
                archived = true;

            _table_value = value ?? 0;
        } }
    private decimal _table_value;


    /// <summary>
    /// Tabela de resgates
    /// </summary>
    [NotMapped]
    public List<Calculated>? table_calculated { get; set; }



    public Investment(){}

    public Investment(InvestmentRequest request, User owner, Bank bank)
    {
        this.owner = owner;
        this.title = request.title;
        this.investment_type = request.investment_type;
        this.bank = bank;
        this.value = request.value;
        this.index = request.index;
        this.index_percent = request.index_percent;
        this.index_value = request.index_value;
        this.taxes = request.taxes;
        this.archived = request.archived;
        this.date_buy = DateTime.SpecifyKind(request.date_buy, DateTimeKind.Utc);
        this.due_date = request.date_expected_sell is null ? null : DateTime.SpecifyKind((DateTime) request.date_expected_sell, DateTimeKind.Utc);
    }


    
    
    public void Update(InvestmentRequest request, Bank bank)
    {
        this.title = request.title;
        this.investment_type = request.investment_type;
        this.bank = bank;
        this.value = request.value;
        this.index = request.index;
        this.index_percent = request.index_percent;
        this.index_value = request.index_value;
        this.taxes = request.taxes;
        this.date_buy = DateTime.SpecifyKind(request.date_buy, DateTimeKind.Utc);
        this.due_date = request.date_expected_sell is null
            ? null
            : DateTime.SpecifyKind((DateTime)request.date_expected_sell, DateTimeKind.Utc);
        this.archived = request.archived;
    }

    internal InvestmentRequest ToRequest()
    {
        return new InvestmentRequest(){
            title = this.title,
            investment_type = this.investment_type,
            bank_code = (int)(this.bank?.Code ?? 0),
            value = this.value,
            index = this.index,
            index_percent = this.index_percent,
            index_value = this.index_value,
            taxes = this.taxes,
            archived = this.archived,
            date_buy = this.date_buy,
            date_expected_sell = this.due_date
        };
    }
}
