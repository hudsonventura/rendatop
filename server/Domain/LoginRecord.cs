namespace server.Domain;

/// <summary>
/// Dados de login
/// </summary>
/// <param name="email">Email do usuario</param>
/// <param name="password">Senha</param>
public record LoginRecord(string email, string password);

