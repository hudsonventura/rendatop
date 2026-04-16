using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

namespace server.Controllers;

[ApiController]
[AllowAnonymous]
public class PublicBlogController : ControllerBase
{
    private readonly Context _context;

    public PublicBlogController(IDbContextFactory<Context> contextFactory)
    {
        _context = contextFactory.CreateDbContext();
    }

    [HttpGet("public/blog/posts")]
    [ProducesResponseType(typeof(PublicBlogPostListResponse), StatusCodes.Status200OK)]
    public ActionResult<PublicBlogPostListResponse> List([FromQuery] int? limit = null)
    {
        IQueryable<BlogPost> query = _context.blog_posts
            .AsNoTracking()
            .Where(post => post.status == BlogPostStatus.Published)
            .OrderByDescending(post => post.published_at ?? post.updated_at);

        if (limit.HasValue && limit.Value > 0)
            query = query.Take(Math.Min(limit.Value, 50));

        var posts = query.ToList();
        var coverAssets = _context.blog_post_assets
            .AsNoTracking()
            .Where(asset => posts.Select(post => post.cover_asset_id).Contains(asset.id))
            .ToDictionary(asset => asset.id, asset => asset);

        var items = posts.Select(post =>
        {
            coverAssets.TryGetValue(post.cover_asset_id ?? Guid.Empty, out var coverAsset);
            var coverImageUrl = BlogRichTextSanitizer.NormalizeDataImageUrl(post.cover_image_data_url)
                ?? (coverAsset is null ? null : BlogUrlBuilder.BuildPublicAssetUrl(coverAsset.id, Request));

            return new PublicBlogPostListItemResponse(
                post.id,
                post.slug,
                post.title,
                post.excerpt,
                post.author_user_name,
                coverImageUrl,
                post.published_at,
                BlogUrlBuilder.BuildPublicPostUrl(post.slug));
        }).ToList();

        return Ok(new PublicBlogPostListResponse(items));
    }

    [HttpGet("public/blog/posts/{slug}")]
    [ProducesResponseType(typeof(PublicBlogPostDetailResponse), StatusCodes.Status200OK)]
    public ActionResult<PublicBlogPostDetailResponse> GetBySlug([FromRoute] string slug)
    {
        var normalizedSlug = (slug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
            return NotFound();

        var post = _context.blog_posts
            .AsNoTracking()
            .FirstOrDefault(item => item.slug == normalizedSlug && item.status == BlogPostStatus.Published);

        if (post is null)
            return NotFound();

        BlogPostAsset? coverAsset = null;
        if (post.cover_asset_id.HasValue)
        {
            coverAsset = _context.blog_post_assets
                .AsNoTracking()
                .FirstOrDefault(asset => asset.id == post.cover_asset_id.Value);
        }
        var coverImageUrl = BlogRichTextSanitizer.NormalizeDataImageUrl(post.cover_image_data_url)
            ?? (coverAsset is null ? null : BlogUrlBuilder.BuildPublicAssetUrl(coverAsset.id, Request));

        return Ok(new PublicBlogPostDetailResponse(
            post.id,
            post.slug,
            post.title,
            post.excerpt,
            BlogRichTextSanitizer.ExpandAssetUrls(post.body_html, assetId => BlogUrlBuilder.BuildPublicAssetUrl(assetId, Request)),
            post.body_text,
            post.author_user_name,
            coverImageUrl,
            post.published_at,
            post.created_at,
            post.updated_at,
            BlogUrlBuilder.BuildPublicPostUrl(post.slug)));
    }

    [HttpGet("public/blog/assets/{assetId}")]
    public IActionResult GetAsset([FromRoute] Guid assetId)
    {
        var asset = _context.blog_post_assets.AsNoTracking().FirstOrDefault(item => item.id == assetId);
        if (asset is null)
            return NotFound();

        var isAdmin = HttpContext.Items.TryGetValue("User", out var userValue) &&
                      userValue is User user &&
                      user.user_type == UserType.Admin;

        if (!isAdmin)
        {
            if (!asset.blog_post_id.HasValue)
                return NotFound();

            var post = _context.blog_posts
                .AsNoTracking()
                .FirstOrDefault(item => item.id == asset.blog_post_id.Value && item.status == BlogPostStatus.Published);

            if (post is null)
                return NotFound();
        }

        Response.Headers["Cache-Control"] = "public,max-age=86400";
        return File(asset.content, asset.content_type, enableRangeProcessing: false);
    }
}

public record PublicBlogPostListResponse(IReadOnlyList<PublicBlogPostListItemResponse> items);

public record PublicBlogPostListItemResponse(
    Guid id,
    string slug,
    string title,
    string excerpt,
    string author_user_name,
    string? cover_image_url,
    DateTime? published_at,
    string public_post_url);

public record PublicBlogPostDetailResponse(
    Guid id,
    string slug,
    string title,
    string excerpt,
    string body_html,
    string body_text,
    string author_user_name,
    string? cover_image_url,
    DateTime? published_at,
    DateTime created_at,
    DateTime updated_at,
    string public_post_url);
