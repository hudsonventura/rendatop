namespace server.RequestObjects;

/// <summary>
/// Objeto usado para arquivar ou desarquivar um investimento.
/// </summary>
public class ArchiveInvestmentRequest
{
    /// <summary>
    /// Indica se o investimento deve ser arquivado.
    /// </summary>
    public bool archived { get; set; }
}
