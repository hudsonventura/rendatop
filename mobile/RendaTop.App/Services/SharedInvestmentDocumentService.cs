using System.Collections.Concurrent;

namespace RendaTop.App.Services;

public sealed record SharedInvestmentDocument(Guid Id, string FilePath, string FileName, string? ContentType);

public sealed class SharedInvestmentDocumentService
{
    private readonly ConcurrentDictionary<Guid, SharedInvestmentDocument> _documents = new();

    public bool IsNavigationReady { get; private set; }

    public event EventHandler<Guid>? DocumentReceived;

    public void Add(string filePath, string fileName, string? contentType)
    {
        var document = new SharedInvestmentDocument(Guid.NewGuid(), filePath, fileName, contentType);
        _documents[document.Id] = document;

        if (IsNavigationReady)
            DocumentReceived?.Invoke(this, document.Id);
    }

    public void MarkNavigationReady() => IsNavigationReady = true;

    public bool TryGetPendingDocumentId(out Guid documentId)
    {
        documentId = _documents.Keys.FirstOrDefault();
        return documentId != Guid.Empty;
    }

    public bool TryTake(Guid documentId, out SharedInvestmentDocument? document)
        => _documents.TryRemove(documentId, out document);

    public void DeleteCachedFile(SharedInvestmentDocument document)
    {
        try
        {
            if (File.Exists(document.FilePath))
                File.Delete(document.FilePath);
        }
        catch
        {
            // The operating system clears this cache if the file cannot be removed now.
        }
    }
}
