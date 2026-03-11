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
    /// Preferência para receber notificações via WhatsApp
    /// </summary>
    public bool notify_whatsapp { get; set; } = false;

    /// <summary>
    /// Preferência para receber notificações via Telegram
    /// </summary>
    public bool notify_telegram { get; set; } = true;

    /// <summary>
    /// Preferência para receber notificações via Email
    /// </summary>
    public bool notify_email { get; set; } = true;

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
