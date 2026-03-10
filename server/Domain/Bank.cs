using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace server.Domain;

/// <summary>
/// Classe que representa um banco
/// </summary>

[Table("banks")]
public class Bank
{
    /// <summary>
    /// Identificador único do banco
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Código do banco
    /// </summary>
    [Column("code")]
    public ushort Code { get; set; } = 0;

    /// <summary>
    /// CNPJ do banco
    /// </summary>
    [Column("cnpj")]
    public string Cnpj { get; set; } = string.Empty;
    
    /// <summary>
    /// Status do banco
    /// </summary>
    [Column("active")]
    public bool Active { get; set; } = true;

    /// <summary>
    /// Nome do banco
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nome fantasia do banco
    /// </summary>
    [Column("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Cor usada para representar o banco em gráficos
    /// </summary>
    [Column("color")]
    public string Color { get; set; } = "#94a3b8";

}
