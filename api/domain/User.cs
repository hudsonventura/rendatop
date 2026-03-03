using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.domain;

[Table("user")]
public class User
{
    /// <summary>
    /// Id do usuário
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Nome do usuário
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email do usuário
    /// </summary>
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário
    /// </summary>
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Salt para criptografia
    /// </summary>
    [Column("salt")]
    public string Salt { get; set; } = string.Empty;
}
