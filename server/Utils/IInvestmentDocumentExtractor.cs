using server.Domain;

namespace server.Utils;

public interface IInvestmentDocumentExtractor
{
    Task<InvestmentDocumentExtractionResult> ExtractAsync(
        IFormFile file,
        IReadOnlyCollection<Bank> banks,
        CancellationToken cancellationToken = default
    );
}
