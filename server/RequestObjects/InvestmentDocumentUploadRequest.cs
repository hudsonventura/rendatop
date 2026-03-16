namespace server.RequestObjects;

public class InvestmentDocumentUploadRequest
{
    public IFormFile file { get; set; } = default!;
}
