using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Controllers;
using server.Domain;
using server.Services;

namespace tests;

public class AdminBlogControllerTests
{
    [Fact]
    public async Task Create_CreatesDraftSanitizesHtmlAndAssociatesAssets()
    {
        using var fixture = new BlogFixture();
        var uploadedAsset = await fixture.UploadAssetAsync("cover.png");
        var controller = fixture.CreateAdminController(fixture.AdminUser);

        var result = await controller.Create(new SaveBlogPostRequest
        {
            title = "Análise <script>alert(1)</script> do CDI",
            excerpt = "Resumo inicial",
            body_html = $"<p>Conteúdo <strong>rico</strong></p><img src=\"/public/blog/assets/{uploadedAsset.id}\" alt=\"Cover\">",
            cover_asset_id = uploadedAsset.id
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<BlogPostDetailResponse>(created.Value);

        Assert.Equal(BlogPostStatus.Draft, response.status);
        Assert.Equal("analise-do-cdi", response.slug);
        Assert.DoesNotContain("script", response.body_html, StringComparison.OrdinalIgnoreCase);
        Assert.Single(response.assets);
        Assert.Equal(uploadedAsset.id, response.cover_asset_id);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedPost = assertionContext.blog_posts.Single();
        var savedAsset = assertionContext.blog_post_assets.Single();

        Assert.Equal(BlogPostStatus.Draft, savedPost.status);
        Assert.Equal(savedPost.id, savedAsset.blog_post_id);
    }

    [Fact]
    public async Task Create_GeneratesUniqueSlugForDuplicateTitles()
    {
        using var fixture = new BlogFixture();
        fixture.SeedPost("Post repetido", BlogPostStatus.Draft);
        var controller = fixture.CreateAdminController(fixture.AdminUser);

        var result = await controller.Create(new SaveBlogPostRequest
        {
            title = "Post repetido",
            body_html = "<p>Outro conteúdo</p>"
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<BlogPostDetailResponse>(created.Value);

        Assert.Equal("post-repetido-2", response.slug);
    }

    [Fact]
    public async Task Publish_PersistsPublishedStatusAndSocialResults()
    {
        using var fixture = new BlogFixture();
        var post = fixture.SeedPost("Carteira mensal", BlogPostStatus.Draft);
        fixture.SeedAsset(post, "cover.png");
        var controller = fixture.CreateAdminController(fixture.AdminUser);

        var result = await controller.Publish(post.id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BlogPostDetailResponse>(ok.Value);

        Assert.Equal(BlogPostStatus.Published, response.status);
        Assert.NotNull(response.published_at);
        Assert.Equal(3, response.social_publications.Count);
        Assert.All(response.social_publications, item => Assert.Equal(SocialPublicationStatus.Published, item.status));

        using var assertionContext = fixture.CreateAssertionContext();
        Assert.Equal(3, assertionContext.blog_post_social_publications.Count());
    }

    [Fact]
    public void PublicEndpoints_ExposeOnlyPublishedPosts()
    {
        using var fixture = new BlogFixture();
        var published = fixture.SeedPost("Publicado", BlogPostStatus.Published);
        fixture.SeedPost("Rascunho", BlogPostStatus.Draft);

        var publicController = fixture.CreatePublicController();
        var list = Assert.IsType<OkObjectResult>(publicController.List().Result);
        var listResponse = Assert.IsType<PublicBlogPostListResponse>(list.Value);

        Assert.Single(listResponse.items);
        Assert.Equal(published.slug, listResponse.items[0].slug);

        var detail = Assert.IsType<OkObjectResult>(publicController.GetBySlug(published.slug).Result);
        var detailResponse = Assert.IsType<PublicBlogPostDetailResponse>(detail.Value);
        Assert.Equal(published.title, detailResponse.title);
    }

    [Fact]
    public void PublicAsset_BlocksUnpublishedForAnonymousAndAllowsAdminPreview()
    {
        using var fixture = new BlogFixture();
        var post = fixture.SeedPost("Rascunho privado", BlogPostStatus.Draft);
        var asset = fixture.SeedAsset(post, "private.png");

        var anonymousController = fixture.CreatePublicController();
        Assert.IsType<NotFoundResult>(anonymousController.GetAsset(asset.id));

        var adminController = fixture.CreatePublicController(fixture.AdminUser);
        var fileResult = Assert.IsType<FileContentResult>(adminController.GetAsset(asset.id));
        Assert.Equal("image/png", fileResult.ContentType);
    }

    [Fact]
    public async Task RetrySocial_UpdatesOnlyRequestedChannel()
    {
        using var fixture = new BlogFixture();
        var post = fixture.SeedPost("Retry social", BlogPostStatus.Published);
        fixture.SeedAsset(post, "cover.png");
        fixture.FakePublisher.NextSingleResult = SocialPublishResult.Failed(SocialChannel.LinkedIn, "Falha temporária");
        var controller = fixture.CreateAdminController(fixture.AdminUser);

        var result = await controller.RetrySocial(post.id, SocialChannel.LinkedIn, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BlogPostDetailResponse>(ok.Value);
        var linkedIn = response.social_publications.Single(item => item.channel == SocialChannel.LinkedIn);

        Assert.Equal(SocialPublicationStatus.Failed, linkedIn.status);
        Assert.Equal("Falha temporária", linkedIn.error_message);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class BlogFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public FakeBlogSocialPublisher FakePublisher { get; } = new();
        public User AdminUser { get; }
        public User CommonUser { get; }

        public BlogFixture()
        {
            _options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

            Context = new Context(_options);
            AdminUser = SeedUser("admin@example.com", "Admin", UserType.Admin);
            CommonUser = SeedUser("user@example.com", "Usuário", UserType.Common);
        }

        public AdminBlogController CreateAdminController(User user)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = user;
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost:5000");

            var controller = new AdminBlogController(
                new HttpContextAccessor { HttpContext = httpContext },
                new TestContextFactory(_options),
                FakePublisher);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        public PublicBlogController CreatePublicController(User? user = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost:5000");

            if (user is not null)
                httpContext.Items["User"] = user;

            var controller = new PublicBlogController(new TestContextFactory(_options))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            return controller;
        }

        public async Task<BlogPostAssetResponse> UploadAssetAsync(string fileName)
        {
            var controller = CreateAdminController(AdminUser);
            var result = await controller.UploadAsset(new UploadBlogAssetRequest
            {
                image = CreateFormFile(fileName, "image/png", "fake-image"),
                alt_text = "Imagem"
            }, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            return Assert.IsType<BlogPostAssetResponse>(ok.Value);
        }

        public User SeedUser(string email, string name, UserType userType)
        {
            var user = new User
            {
                name = name,
                email = email,
                password = "secret",
                user_type = userType,
                auth_provider = AuthProvider.Password
            };

            Context.users.Add(user);
            Context.SaveChanges();
            return user;
        }

        public BlogPost SeedPost(string title, BlogPostStatus status)
        {
            var now = DateTime.UtcNow.AddMinutes(-5);
            var post = new BlogPost
            {
                title = title,
                slug = server.Utils.BlogSlug.Create(title),
                excerpt = "Resumo",
                body_html = "<p>Conteúdo</p>",
                body_text = "Conteúdo",
                status = status,
                author_user_id = AdminUser.id,
                author_user_name = AdminUser.name,
                published_at = status == BlogPostStatus.Published ? now : null,
                created_at = now,
                updated_at = now
            };

            Context.blog_posts.Add(post);
            Context.SaveChanges();
            return post;
        }

        public BlogPostAsset SeedAsset(BlogPost post, string fileName)
        {
            var asset = new BlogPostAsset
            {
                blog_post_id = post.id,
                file_name = fileName,
                content_type = "image/png",
                size_bytes = 12,
                content = Encoding.UTF8.GetBytes("image-bytes"),
                alt_text = "Imagem",
                created_at = DateTime.UtcNow.AddMinutes(-4)
            };

            Context.blog_post_assets.Add(asset);
            post.cover_asset_id = asset.id;
            Context.SaveChanges();
            return asset;
        }

        public Context CreateAssertionContext() => new(_options);

        public void Dispose()
        {
            Context.Dispose();
        }
    }

    private sealed class TestContextFactory : IDbContextFactory<Context>
    {
        private readonly DbContextOptions<Context> _options;

        public TestContextFactory(DbContextOptions<Context> options)
        {
            _options = options;
        }

        public Context CreateDbContext()
        {
            return new Context(_options);
        }
    }

    private sealed class FakeBlogSocialPublisher : IBlogSocialPublisher
    {
        public SocialPublishResult? NextSingleResult { get; set; }

        public Task<IReadOnlyList<SocialPublishResult>> PublishAllAsync(SocialPublishRequest request, CancellationToken cancellationToken)
        {
            IReadOnlyList<SocialPublishResult> results =
            [
                SocialPublishResult.Published(SocialChannel.Facebook, "fb-1", "https://facebook.example/post"),
                SocialPublishResult.Published(SocialChannel.Instagram, "ig-1", null),
                SocialPublishResult.Published(SocialChannel.LinkedIn, "li-1", "https://linkedin.example/post")
            ];

            return Task.FromResult(results);
        }

        public Task<SocialPublishResult> PublishAsync(SocialChannel channel, SocialPublishRequest request, CancellationToken cancellationToken)
        {
            var result = NextSingleResult ?? SocialPublishResult.Published(channel, $"{channel}-1", null);
            NextSingleResult = null;
            return Task.FromResult(result);
        }
    }
}

