using server.Domain;

namespace server.Utils;

public class InvestmentDocumentExtractorRouter : IInvestmentDocumentExtractor
{
    private readonly OpenAiInvestmentDocumentExtractor _openAiExtractor;

    public InvestmentDocumentExtractorRouter(OpenAiInvestmentDocumentExtractor openAiExtractor)
    {
        _openAiExtractor = openAiExtractor;
    }

    public Task<InvestmentDocumentExtractionResult> ExtractAsync(
        IFormFile file,
        IReadOnlyCollection<Bank> banks,
        CancellationToken cancellationToken = default
    )
    {
        var provider = (Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "openai").Trim().ToLowerInvariant();

        return provider switch
        {
            "openai" => _openAiExtractor.ExtractAsync(file, banks, cancellationToken),
            "gemini" => throw new ExpectedException("LLM_PROVIDER=gemini ainda não foi implementado. A interface já está pronta para isso."),
            _ => throw new ExpectedException($"LLM_PROVIDER inválido: {provider}")
        };
    }
}
