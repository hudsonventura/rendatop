using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.domain;

/// <summary>
/// Objeto do tipo investimento renda fixa (CDB, LCI/LCA, etc)
/// </summary>
public class Investment
{
    /// <summary>
    /// Chave primaria
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = SnowflakeGuid.NewGuid();

    /// <summary>
    /// Dono do investimento
    /// </summary>
    [Column("owner")]
    public User Owner { get; set; } = null!;

    /// <summary>
    /// Uma identificação sobre o investimento
    /// </summary>
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// String que represta o banco onde o investimento foi feito
    /// </summary>
    [Column("bank")]
    public string Bank { get; set; } = string.Empty;

    /// <summary>
    /// Data de compra
    /// </summary>
    [Column("date_buy")]
    public DateTime DateBuy { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Data esperada da venda/liquidação em conta. Pode ser vazia
    /// </summary>
    [Column("date_expected_sell")]
    public DateTime? DateExpectedSell { get; set; } = null;

    /// <summary>
    /// Valor do investido
    /// </summary>
    [Column("value")]
    public decimal Value { get; set; } = 0;

    /// <summary>
    /// Indice associado ao investimento. Ex.: %a.a., CDI, IPCA, IPCA + x%, etc.
    /// </summary>
    [Column("index")]
    public IdexerType Index { get; set; } = IdexerType.Cdi;

    /// <summary>
    /// Percentual do índice. Ex.: 12% ao ano, 110% do CDI ou 96% do IPCA
    /// </summary>
    [Column("index_percent")]
    public decimal IndexPercent { get; set; } = 0;

    /// <summary>
    /// Valor excedente a ser somado. Usar em casos, por exemplo de IPCA + 5%. Default 0
    /// </summary>
    [Column("index_value")]
    public decimal IndexValue { get; set; } = 0;

    /// <summary>
    /// Indica se o investimento possui ou não a incidência de impostos. Os impostos são calculados com base na tabela de IR
    /// </summary>
    [Column("taxes")]
    public bool Taxes { get; set; } = true;


    //[NotMapped] //impede que vá para o entity
    //public List<Calculated> calculated { get; set; }



}