using System.Security.Cryptography;

namespace api.services;

/// <summary>
/// Classe responsável por criptografar e verificar senhas usando PBKDF2 com SHA-512.
/// PBKDF2 (Password-Based Key Derivation Function 2) aplica SHA-512 iterativamente,
/// tornando ataques de força bruta computacionalmente inviáveis.
/// </summary>
public static class Encrypt
{
    /// <summary>
    /// Número de iterações do PBKDF2. Quanto maior, mais seguro (e mais lento).
    /// 210.000 é o mínimo recomendado pelo OWASP para SHA-512.
    /// </summary>
    private const int Iterations = 210_000;

    /// <summary>
    /// Tamanho do hash em bytes (64 bytes = 512 bits).
    /// </summary>
    private const int HashSize = 64;

    /// <summary>
    /// Hash de senha usando PBKDF2 com SHA-512.
    /// Retorna o hash em Base64.
    /// </summary>
    /// <param name="password">Senha em texto puro</param>
    /// <param name="salt">Salt único do usuário</param>
    /// <returns>Hash da senha em Base64</returns>
    public static string HashPassword(string password, string salt)
    {
        byte[] saltBytes = System.Text.Encoding.UTF8.GetBytes(salt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: saltBytes,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA512,
            outputLength: HashSize
        );

        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifica se a senha informada corresponde ao hash armazenado.
    /// Usa comparação em tempo constante para prevenir ataques de timing.
    /// </summary>
    /// <param name="password">Senha em texto puro</param>
    /// <param name="salt">Salt único do usuário</param>
    /// <param name="storedHash">Hash armazenado no banco de dados</param>
    /// <returns>True se a senha for válida</returns>
    public static bool VerifyPassword(string password, string salt, string storedHash)
    {
        string computedHash = HashPassword(password, salt);

        // Comparação em tempo constante para evitar timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computedHash),
            Convert.FromBase64String(storedHash)
        );
    }
}
