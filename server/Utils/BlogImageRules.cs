using System.Net;

namespace server.Utils;

public sealed record BlogValidatedImage(
    string FileName,
    string ContentType,
    long SizeBytes,
    byte[] Content,
    string AltText);

public static class BlogImageRules
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".gif"
    };

    public static async Task<BlogValidatedImage> ValidateAsync(IFormFile file, string? altText, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new ExpectedException("Imagem é obrigatória.", HttpStatusCode.BadRequest);

        if (file.Length <= 0)
            throw new ExpectedException("A imagem enviada está vazia.", HttpStatusCode.BadRequest);

        if (file.Length > MaxFileSizeBytes)
            throw new ExpectedException("Cada imagem do blog pode ter no máximo 5 MB.", HttpStatusCode.BadRequest);

        var fileName = Path.GetFileName(file.FileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ExpectedException("Nome do arquivo da imagem é obrigatório.", HttpStatusCode.BadRequest);

        var extension = Path.GetExtension(fileName);
        var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(contentType))
            throw new ExpectedException("Formato de imagem não permitido. Use PNG, JPG, WEBP ou GIF.", HttpStatusCode.BadRequest);

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return new BlogValidatedImage(
            fileName,
            contentType,
            file.Length,
            stream.ToArray(),
            (altText ?? string.Empty).Trim());
    }
}
