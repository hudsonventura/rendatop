using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace server.Domain;

/// <summary>
/// Objeto do tipo usuário
/// </summary>
public class User
{
    /// <summary>
    /// Código ID do usuário (Chave primaria)
    /// </summary>
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    /// <summary>
    /// Nome do usuário
    /// </summary>
    public string name { get; set; }


    /// <summary>
    /// Email de uso único para o usuário
    /// </summary>
    public string email { get; set; }

    /// <summary>
    /// Telefone do usuário no formato 99999999999
    /// </summary>
    public string phone { get; set; } = string.Empty;

    /// <summary>
    /// CPF do usuário no formato 00000000000
    /// </summary>
    public string cpf { get; set; } = string.Empty;

    /// <summary>
    /// Preferência para receber notificações via WhatsApp
    /// </summary>
    public bool notify_whatsapp { get; set; } = false;

    /// <summary>
    /// Preferência para receber notificações via Telegram
    /// </summary>
    public bool notify_telegram { get; set; } = false;

    /// <summary>
    /// Preferência para receber notificações via Email
    /// </summary>
    public bool notify_email { get; set; } = false;

    /// <summary>
    /// Habilita o compartilhamento público do calendário de vencimentos (.ics)
    /// </summary>
    public bool calendar_public_enabled { get; set; } = false;

    /// <summary>
    /// Token público usado no link do calendário. Não é o id do usuário.
    /// </summary>
    public Guid? calendar_public_token { get; set; }

    /// <summary>
    /// Define se o login comum (email/senha) exige TOTP
    /// </summary>
    public bool totp_enabled { get; set; } = false;

    /// <summary>
    /// Chave secreta TOTP em base32
    /// </summary>
    [JsonIgnore]
    public string? totp_secret { get; set; }

    /// <summary>
    /// Salt para encriptação da senha
    /// </summary>
    public string salt { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Senha do usuário
    /// </summary>
    [JsonIgnore]
    public string password { 
        get{
            return Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(_pass + salt)));
        } 
        set{
            _pass = value ?? string.Empty;
        }
    }
    private string _pass;
    

    public bool CheckPass(string password){
        var hash = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(password + salt)));
        if(hash == _pass){
            return true;
        }
        return false;
    }

    public string GetJsonSerialized(){
        User temp = this;
        temp.password = string.Empty;
        return JsonSerializer.Serialize(temp);
    }

    public static User Deserialize(string json){
        return JsonSerializer.Deserialize<User>(json);
    }
}
