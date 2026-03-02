using BCrypt;

namespace api.services;

/// <summary>
/// Classe responsável por criptografar e descriptografar senhas
/// </summary>
public static class Encrypt
{
    /// <summary>
    /// Hash de senha
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    public static string HashPassword(string password, string salt)
    {
        string passwordWithSalt = password + salt;
        return BCrypt.Net.BCrypt.HashPassword(passwordWithSalt);
    }

    /// <summary>
    /// Verifica se a senha é válida
    /// </summary>
    /// <param name="password"></param>
    /// <param name="hash"></param>
    /// <returns></returns>
    public static bool VerifyPassword(string password, string salt, string hash)
    {
        string passwordWithSalt = password + salt;
        return BCrypt.Net.BCrypt.Verify(passwordWithSalt, hash);
    }
}
