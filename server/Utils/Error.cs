namespace server.Domain;

/// <summary>
/// Objeto padrão de retorno de ERRO
/// </summary>
public class Error
{
    /// <summary>
    /// StatusCode HTTP
    /// </summary>
    public int StatusCode { get; set; } = 400;

    /// <summary>
    /// Informação sobre o erro
    /// </summary>
    public string? Message { get; set; }


    /// <summary>
    /// Data e hora do erro em UTC
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

