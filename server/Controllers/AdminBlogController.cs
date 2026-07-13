using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Domain;
using server.Services;
using server.Utils;
using System.Text.RegularExpressions;

namespace server.Controllers;

[ApiController]
public class AdminBlogController : AuthenticatedController
{
    private const int BlogExcerptMaxLength = 2200;
    private readonly Context _context;
    private readonly IBlogSocialPublisher _socialPublisher;
    private readonly ITemporarySocialAssetService _temporarySocialAssetService;
    private readonly ILogger<AdminBlogController> _logger;

    public AdminBlogController(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<Context> contextFactory,
        IBlogSocialPublisher socialPublisher,
        ITemporarySocialAssetService temporarySocialAssetService,
        ILogger<AdminBlogController> logger) : base(httpContextAccessor)
    {
        _context = contextFactory.CreateDbContext();
        _socialPublisher = socialPublisher;
        _temporarySocialAssetService = temporarySocialAssetService;
        _logger = logger;
    }

    [HttpGet("admin/blog/posts")]
    [ProducesResponseType(typeof(BlogPostListResponse), StatusCodes.Status200OK)]
    public ActionResult<BlogPostListResponse> List([FromQuery] string scope = "all")
    {
        EnsureAdmin();

        var query = _context.blog_posts.AsNoTracking().AsQueryable();

        if (scope.Equals("draft", StringComparison.OrdinalIgnoreCase))
            query = query.Where(post => post.status == BlogPostStatus.Draft);
        else if (scope.Equals("published", StringComparison.OrdinalIgnoreCase))
            query = query.Where(post => post.status == BlogPostStatus.Published);

        var posts = query
            .OrderByDescending(post => post.published_at ?? post.updated_at)
            .ThenByDescending(post => post.updated_at)
            .ToList();

        var socialPublications = _context.blog_post_social_publications
            .AsNoTracking()
            .Where(publication => posts.Select(post => post.id).Contains(publication.blog_post_id))
            .ToList();

        var items = posts.Select(post => BuildSummaryResponse(post, socialPublications.Where(item => item.blog_post_id == post.id).ToList()))
            .ToList();

        return Ok(new BlogPostListResponse(items));
    }

    [HttpGet("admin/blog/posts/{id}")]
    [ProducesResponseType(typeof(BlogPostDetailResponse), StatusCodes.Status200OK)]
    public ActionResult<BlogPostDetailResponse> Get([FromRoute] Guid id)
    {
        EnsureAdmin();

        var post = GetBlogPostOrThrow(id);
        return Ok(BuildDetailResponse(post));
    }

    [HttpPost("admin/blog/posts")]
    [ProducesResponseType(typeof(BlogPostDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BlogPostDetailResponse>> Create(
        [FromBody] SaveBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var normalized = await NormalizeSaveRequestAsync(request, null, cancellationToken);
        var now = DateTime.UtcNow;

        var post = new BlogPost
        {
            slug = normalized.Slug,
            title = normalized.Title,
            excerpt = normalized.Excerpt,
            body_html = normalized.BodyHtml,
            body_text = normalized.BodyText,
            status = BlogPostStatus.Draft,
            author_user_id = _user.id,
            author_user_name = _user.name,
            cover_image_data_url = normalized.CoverImageDataUrl,
            cover_asset_id = normalized.CoverAssetId,
            created_at = now,
            updated_at = now
        };

        _context.blog_posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        await SynchronizeAssetsAsync(post.id, normalized.AssetIds, normalized.CoverAssetId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = post.id }, BuildDetailResponse(post));
    }

    [HttpPut("admin/blog/posts/{id}")]
    [ProducesResponseType(typeof(BlogPostDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlogPostDetailResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] SaveBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var post = GetBlogPostOrThrow(id);
        var normalized = await NormalizeSaveRequestAsync(request, post, cancellationToken);

        post.slug = normalized.Slug;
        post.title = normalized.Title;
        post.excerpt = normalized.Excerpt;
        post.body_html = normalized.BodyHtml;
        post.body_text = normalized.BodyText;
        post.cover_image_data_url = normalized.CoverImageDataUrl;
        post.cover_asset_id = normalized.CoverAssetId;
        post.updated_at = DateTime.UtcNow;

        await SynchronizeAssetsAsync(post.id, normalized.AssetIds, normalized.CoverAssetId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(BuildDetailResponse(post));
    }

    [HttpPost("admin/blog/posts/{id}/publish")]
    [ProducesResponseType(typeof(BlogPostDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlogPostDetailResponse>> Publish([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var post = GetBlogPostOrThrow(id);
        if (post.status == BlogPostStatus.Published)
            return Ok(BuildDetailResponse(post));

        var now = DateTime.UtcNow;
        post.status = BlogPostStatus.Published;
        post.published_at = now;
        post.updated_at = now;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(BuildDetailResponse(post));
    }

    [HttpPost("admin/blog/posts/{id}/social/{channel}/retry")]
    [ProducesResponseType(typeof(BlogPostDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlogPostDetailResponse>> RetrySocial(
        [FromRoute] Guid id,
        [FromRoute] SocialChannel channel,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var post = GetBlogPostOrThrow(id);
        if (post.status != BlogPostStatus.Published)
            throw new ExpectedException("Somente postagens publicadas podem ser reenviadas às redes sociais.", HttpStatusCode.BadRequest);

        var result = await _socialPublisher.PublishAsync(channel, BuildSocialPublishRequest(post), cancellationToken);
        if (result.status == SocialPublicationStatus.Failed)
        {
            _logger.LogWarning("Falha ao publicar post {PostId} na rede {Channel}. Motivo: {ErrorMessage}", post.id, channel, result.error_message);
        }
        else
        {
            _logger.LogInformation("Post {PostId} publicado com sucesso na rede {Channel}. RemoteId: {RemotePostId}", post.id, channel, result.remote_post_id);
        }

        UpsertSocialPublicationResults(post.id, [result]);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(BuildDetailResponse(post));
    }

    [HttpPost("admin/blog/assets")]
    [ProducesResponseType(typeof(BlogPostAssetResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlogPostAssetResponse>> UploadAsset(
        [FromForm] UploadBlogAssetRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var validated = await BlogImageRules.ValidateAsync(request.image, request.alt_text, cancellationToken);
        var asset = new BlogPostAsset
        {
            file_name = validated.FileName,
            content_type = validated.ContentType,
            size_bytes = validated.SizeBytes,
            content = validated.Content,
            alt_text = validated.AltText,
            created_at = DateTime.UtcNow
        };

        _context.blog_post_assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(BuildAssetResponse(asset));
    }

    private BlogPost GetBlogPostOrThrow(Guid id)
    {
        var post = _context.blog_posts.FirstOrDefault(item => item.id == id);
        if (post is null)
            throw new ExpectedException("Postagem não encontrada.", HttpStatusCode.NotFound);

        return post;
    }

    private async Task<NormalizedSaveBlogPostRequest> NormalizeSaveRequestAsync(
        SaveBlogPostRequest request,
        BlogPost? existingPost,
        CancellationToken cancellationToken)
    {
        var title = StripHtml(request.title);
        if (string.IsNullOrWhiteSpace(title))
            throw new ExpectedException("Título é obrigatório.", HttpStatusCode.BadRequest);

        if (title.Length > 180)
            throw new ExpectedException("Título pode ter no máximo 180 caracteres.", HttpStatusCode.BadRequest);

        var sanitizedHtml = BlogRichTextSanitizer.Sanitize(request.body_html);
        var bodyText = BlogRichTextSanitizer.ToPlainText(sanitizedHtml);
        if (string.IsNullOrWhiteSpace(bodyText))
            throw new ExpectedException("Conteúdo da postagem é obrigatório.", HttpStatusCode.BadRequest);

        var excerpt = StripHtml(request.excerpt);
        if (string.IsNullOrWhiteSpace(excerpt))
            excerpt = Truncate(bodyText, BlogExcerptMaxLength);
        else if (excerpt.Length > BlogExcerptMaxLength)
            excerpt = excerpt[..BlogExcerptMaxLength].TrimEnd();

        var normalizedCoverImageDataUrl = BlogRichTextSanitizer.NormalizeDataImageUrl(request.cover_image_data_url);

        var assetIds = BlogRichTextSanitizer.ExtractAssetIds(sanitizedHtml).ToHashSet();
        if (request.cover_asset_id.HasValue)
            assetIds.Add(request.cover_asset_id.Value);

        if (assetIds.Count > 0)
        {
            var knownIds = await _context.blog_post_assets
                .AsNoTracking()
                .Where(asset => assetIds.Contains(asset.id))
                .Select(asset => asset.id)
                .ToListAsync(cancellationToken);

            var unknownIds = assetIds.Except(knownIds).ToList();
            if (unknownIds.Count > 0)
                throw new ExpectedException("Uma ou mais imagens da postagem não foram encontradas.", HttpStatusCode.BadRequest);
        }

        var baseSlug = BlogSlug.Create(title);
        var slug = await EnsureUniqueSlugAsync(baseSlug, existingPost?.id, cancellationToken);

        return new NormalizedSaveBlogPostRequest(
            title,
            excerpt,
            sanitizedHtml,
            bodyText,
            slug,
            assetIds.ToList(),
            normalizedCoverImageDataUrl,
            request.cover_asset_id);
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? currentPostId, CancellationToken cancellationToken)
    {
        var slug = baseSlug;
        var suffix = 2;

        while (await _context.blog_posts.AnyAsync(
                   post => post.slug == slug && (!currentPostId.HasValue || post.id != currentPostId.Value),
                   cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private async Task SynchronizeAssetsAsync(
        Guid postId,
        IReadOnlyCollection<Guid> assetIds,
        Guid? coverAssetId,
        CancellationToken cancellationToken)
    {
        var desiredIds = new HashSet<Guid>(assetIds);
        if (coverAssetId.HasValue)
            desiredIds.Add(coverAssetId.Value);

        var currentAssets = await _context.blog_post_assets
            .Where(asset => asset.blog_post_id == postId)
            .ToListAsync(cancellationToken);

        foreach (var currentAsset in currentAssets.Where(asset => !desiredIds.Contains(asset.id)))
            currentAsset.blog_post_id = null;

        if (desiredIds.Count == 0)
            return;

        var desiredAssets = await _context.blog_post_assets
            .Where(asset => desiredIds.Contains(asset.id))
            .ToListAsync(cancellationToken);

        foreach (var asset in desiredAssets)
            asset.blog_post_id = postId;
    }

    private SocialPublishRequest BuildSocialPublishRequest(BlogPost post)
    {
        var socialAssets = LoadSocialAssets(post);
        return new SocialPublishRequest(
            post.id,
            post.slug,
            post.title,
            post.excerpt,
            post.body_html,
            post.body_text,
            BlogUrlBuilder.BuildPublicPostUrl(post.slug),
            socialAssets);
    }

    private IReadOnlyList<SocialPublishImage> LoadSocialAssets(BlogPost post)
    {
        var assets = _context.blog_post_assets
            .AsNoTracking()
            .Where(asset => asset.blog_post_id == post.id)
            .OrderBy(asset => asset.id == post.cover_asset_id ? 0 : 1)
            .ThenBy(asset => asset.created_at)
            .ToList();

        if (!string.IsNullOrWhiteSpace(post.cover_image_data_url))
        {
            var coverImage = TryCreateSocialImageFromDataUrl(post.cover_image_data_url, Request);
            if (coverImage is not null)
                return [coverImage, .. assets.Select(BuildTemporarySocialImage)];
        }

        return assets
            .Select(BuildTemporarySocialImage)
            .ToList();
    }

    private SocialPublishImage BuildTemporarySocialImage(BlogPostAsset asset)
        => new(
            asset.id,
            asset.file_name,
            asset.content_type,
            asset.alt_text,
            asset.content,
            _temporarySocialAssetService.Store(asset.file_name, asset.content_type, asset.content, Request));

    private void UpsertSocialPublicationResults(Guid postId, IReadOnlyList<SocialPublishResult> results)
    {
        var existing = _context.blog_post_social_publications
            .Where(publication => publication.blog_post_id == postId)
            .ToList();

        foreach (var result in results)
        {
            var now = DateTime.UtcNow;
            var publication = existing.FirstOrDefault(item => item.channel == result.channel);

            if (publication is null)
            {
                publication = new BlogPostSocialPublication
                {
                    blog_post_id = postId,
                    channel = result.channel,
                    created_at = now
                };

                _context.blog_post_social_publications.Add(publication);
                existing.Add(publication);
            }

            publication.status = result.status;
            publication.remote_post_id = !string.IsNullOrWhiteSpace(result.remote_post_id) ? result.remote_post_id : publication.remote_post_id;
            publication.remote_url = !string.IsNullOrWhiteSpace(result.remote_url) ? result.remote_url : publication.remote_url;
            publication.error_message = result.error_message;
            publication.updated_at = now;
            if (result.status == SocialPublicationStatus.Published)
                publication.published_at = now;
        }
    }

    private BlogPostSummaryResponse BuildSummaryResponse(BlogPost post, IReadOnlyList<BlogPostSocialPublication> socialPublications)
    {
        var coverAssetResponse = BuildCoverResponse(post);

        return new BlogPostSummaryResponse(
            post.id,
            post.slug,
            post.title,
            post.excerpt,
            post.status,
            post.author_user_name,
            coverAssetResponse,
            BuildSocialResponses(socialPublications),
            post.published_at,
            post.updated_at,
            BlogUrlBuilder.BuildPublicPostUrl(post.slug));
    }

    private BlogPostDetailResponse BuildDetailResponse(BlogPost post)
    {
        var assets = _context.blog_post_assets
            .AsNoTracking()
            .Where(asset => asset.blog_post_id == post.id)
            .OrderBy(asset => asset.created_at)
            .ToList();

        var socialPublications = _context.blog_post_social_publications
            .AsNoTracking()
            .Where(publication => publication.blog_post_id == post.id)
            .OrderBy(publication => publication.channel)
            .ToList();
        var coverAssetResponse = BuildCoverResponse(post, assets);

        return new BlogPostDetailResponse(
            post.id,
            post.slug,
            post.title,
            post.excerpt,
            BlogRichTextSanitizer.ExpandAssetUrls(post.body_html, assetId => BlogUrlBuilder.BuildPublicAssetUrl(assetId, Request)),
            post.body_text,
            post.status,
            post.author_user_id,
            post.author_user_name,
            post.cover_asset_id,
            coverAssetResponse,
            assets.Select(BuildAssetResponse).ToList(),
            BuildSocialResponses(socialPublications),
            post.published_at,
            post.created_at,
            post.updated_at,
            BlogUrlBuilder.BuildPublicPostUrl(post.slug));
    }

    private List<BlogSocialPublicationResponse> BuildSocialResponses(IReadOnlyList<BlogPostSocialPublication> existingPublications)
    {
        var byChannel = existingPublications.ToDictionary(item => item.channel);

        return Enum.GetValues<SocialChannel>()
            .OrderBy(channel => (int)channel)
            .Select(channel =>
            {
                if (!byChannel.TryGetValue(channel, out var publication))
                {
                    return new BlogSocialPublicationResponse(
                        channel,
                        SocialPublicationStatus.Pending,
                        null,
                        null,
                        "Aguardando publicação.",
                        null,
                        null);
                }

                return new BlogSocialPublicationResponse(
                    publication.channel,
                    publication.status,
                    publication.remote_post_id,
                    publication.remote_url,
                    publication.error_message,
                    publication.published_at,
                    publication.updated_at);
            })
            .ToList();
    }

    private BlogPostAssetResponse BuildAssetResponse(BlogPostAsset asset)
        => new(
            asset.id,
            asset.file_name,
            asset.content_type,
            asset.size_bytes,
            asset.alt_text,
            BlogUrlBuilder.BuildPublicAssetUrl(asset.id, Request),
            asset.created_at);

    private BlogPostAssetResponse? BuildCoverResponse(BlogPost post, IReadOnlyList<BlogPostAsset>? loadedAssets = null)
    {
        var normalizedDataUrl = BlogRichTextSanitizer.NormalizeDataImageUrl(post.cover_image_data_url);
        if (!string.IsNullOrWhiteSpace(normalizedDataUrl))
        {
            return new BlogPostAssetResponse(
                Guid.Empty,
                "cover-inline",
                GetContentTypeFromDataUrl(normalizedDataUrl),
                EstimateDataUrlBytes(normalizedDataUrl),
                "Capa do post",
                normalizedDataUrl,
                post.updated_at);
        }

        BlogPostAsset? coverAsset = null;
        if (post.cover_asset_id.HasValue)
        {
            coverAsset = loadedAssets?.FirstOrDefault(asset => asset.id == post.cover_asset_id.Value)
                ?? _context.blog_post_assets.AsNoTracking().FirstOrDefault(asset => asset.id == post.cover_asset_id.Value);
        }

        return coverAsset is null ? null : BuildAssetResponse(coverAsset);
    }

    private SocialPublishImage? TryCreateSocialImageFromDataUrl(string? dataUrl, HttpRequest? request = null)
    {
        var normalized = BlogRichTextSanitizer.NormalizeDataImageUrl(dataUrl);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var commaIndex = normalized.IndexOf(',');
        if (commaIndex < 0 || commaIndex >= normalized.Length - 1)
            return null;

        var header = normalized[..commaIndex];
        var base64 = normalized[(commaIndex + 1)..];
        var contentType = GetContentTypeFromDataUrl(normalized);

        try
        {
            var content = Convert.FromBase64String(base64);
            var fileName = $"cover.{GetExtensionFromContentType(contentType)}";
            return new SocialPublishImage(
                Guid.Empty,
                fileName,
                contentType,
                "Capa do post",
                content,
                _temporarySocialAssetService.Store(fileName, contentType, content, request));
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentTypeFromDataUrl(string dataUrl)
    {
        var match = Regex.Match(dataUrl, @"^data:(?<contentType>image\/[a-zA-Z0-9.+-]+);base64,", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["contentType"].Value : "image/png";
    }

    private static string GetExtensionFromContentType(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => "jpg",
        "image/jpg" => "jpg",
        "image/webp" => "webp",
        "image/gif" => "gif",
        _ => "png"
    };

    private static long EstimateDataUrlBytes(string dataUrl)
    {
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex < 0 || commaIndex >= dataUrl.Length - 1)
            return 0;

        var base64Length = dataUrl.Length - commaIndex - 1;
        return (long)Math.Ceiling(base64Length * 3 / 4d);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..maxLength].TrimEnd();
    }

    private static string StripHtml(string? value)
    {
        var input = value ?? string.Empty;
        input = Regex.Replace(input, @"<(script|style)\b[^>]*>.*?</\1>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        input = Regex.Replace(input, "<[^>]+>", " ");
        input = Regex.Replace(input, @"\s{2,}", " ");
        return input.Trim();
    }
}

public sealed class SaveBlogPostRequest
{
    public string? title { get; set; }
    public string? excerpt { get; set; }
    public string? body_html { get; set; }
    public string? cover_image_data_url { get; set; }
    public Guid? cover_asset_id { get; set; }
}

public sealed class UploadBlogAssetRequest
{
    public IFormFile image { get; set; } = null!;
    public string? alt_text { get; set; }
}

internal sealed record NormalizedSaveBlogPostRequest(
    string Title,
    string Excerpt,
    string BodyHtml,
    string BodyText,
    string Slug,
    IReadOnlyList<Guid> AssetIds,
    string? CoverImageDataUrl,
    Guid? CoverAssetId);

public record BlogPostListResponse(IReadOnlyList<BlogPostSummaryResponse> items);

public record BlogPostSummaryResponse(
    Guid id,
    string slug,
    string title,
    string excerpt,
    BlogPostStatus status,
    string author_user_name,
    BlogPostAssetResponse? cover_asset,
    IReadOnlyList<BlogSocialPublicationResponse> social_publications,
    DateTime? published_at,
    DateTime updated_at,
    string public_post_url);

public record BlogPostDetailResponse(
    Guid id,
    string slug,
    string title,
    string excerpt,
    string body_html,
    string body_text,
    BlogPostStatus status,
    Guid author_user_id,
    string author_user_name,
    Guid? cover_asset_id,
    BlogPostAssetResponse? cover_asset,
    IReadOnlyList<BlogPostAssetResponse> assets,
    IReadOnlyList<BlogSocialPublicationResponse> social_publications,
    DateTime? published_at,
    DateTime created_at,
    DateTime updated_at,
    string public_post_url);

public record BlogPostAssetResponse(
    Guid id,
    string file_name,
    string content_type,
    long size_bytes,
    string alt_text,
    string url,
    DateTime created_at);

public record BlogSocialPublicationResponse(
    SocialChannel channel,
    SocialPublicationStatus status,
    string? remote_post_id,
    string? remote_url,
    string? error_message,
    DateTime? published_at,
    DateTime? updated_at);
