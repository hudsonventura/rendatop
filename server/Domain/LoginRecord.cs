namespace server.Domain;

/// <summary>
/// Dados de login
/// </summary>
/// <param name="email">Email do usuario</param>
/// <param name="password">Senha</param>
/// <param name="totp_code">Código TOTP (opcional)</param>
public record LoginRecord(string email, string password, string? totp_code = null);
