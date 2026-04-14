using System.Net;
using Microsoft.AspNetCore.Http;

namespace server.Utils;

public static class SupportAttachmentRules
{
    public const int MaxFileSizeBytes = 1 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    public static async Task<SupportValidatedAttachment> ValidateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            throw new ExpectedException("Anexo inválido ou vazio.", HttpStatusCode.BadRequest);

        if (file.Length > MaxFileSizeBytes)
            throw new ExpectedException("Cada anexo pode ter no máximo 1 MB.", HttpStatusCode.BadRequest);

        var originalFileName = Path.GetFileName(file.FileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ExpectedException("Nome de arquivo inválido.", HttpStatusCode.BadRequest);

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !ContentTypesByExtension.TryGetValue(extension, out var canonicalContentType))
            throw new ExpectedException("Tipo de arquivo não permitido. Envie imagem, PDF ou arquivo Office.", HttpStatusCode.BadRequest);

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        return new SupportValidatedAttachment(
            originalFileName,
            canonicalContentType,
            memoryStream.ToArray(),
            memoryStream.Length,
            canonicalContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record SupportValidatedAttachment(
    string FileName,
    string ContentType,
    byte[] Content,
    long SizeBytes,
    bool IsImage
);
