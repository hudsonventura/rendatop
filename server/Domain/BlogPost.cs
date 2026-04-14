using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Domain;

public enum BlogPostStatus
{
    Draft = 1,
    Published = 2
}

public enum SocialChannel
{
    Facebook = 1,
    Instagram = 2,
    LinkedIn = 3
}

public enum SocialPublicationStatus
{
    Pending = 1,
    Published = 2,
    Failed = 3
}

[Table("blog_posts")]
public class BlogPost
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    public string slug { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string excerpt { get; set; } = string.Empty;
    public string body_html { get; set; } = string.Empty;
    public string body_text { get; set; } = string.Empty;
    public BlogPostStatus status { get; set; } = BlogPostStatus.Draft;

    [ForeignKey("author_user_id")]
    [JsonIgnore]
    public User author_user { get; set; } = null!;
    public Guid author_user_id { get; set; }

    public string author_user_name { get; set; } = string.Empty;
    public Guid? cover_asset_id { get; set; }
    public DateTime? published_at { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<BlogPostAsset>? assets { get; set; }

    [JsonIgnore]
    public ICollection<BlogPostSocialPublication>? social_publications { get; set; }
}

[Table("blog_post_assets")]
public class BlogPostAsset
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("blog_post_id")]
    [JsonIgnore]
    public BlogPost? blog_post { get; set; }
    public Guid? blog_post_id { get; set; }

    public string file_name { get; set; } = string.Empty;
    public string content_type { get; set; } = string.Empty;
    public long size_bytes { get; set; }
    public byte[] content { get; set; } = [];
    public string alt_text { get; set; } = string.Empty;
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}

[Table("blog_post_social_publications")]
public class BlogPostSocialPublication
{
    [Key]
    public Guid id { get; set; } = SnowflakeGuid.NewGuid();

    [ForeignKey("blog_post_id")]
    [JsonIgnore]
    public BlogPost blog_post { get; set; } = null!;
    public Guid blog_post_id { get; set; }

    public SocialChannel channel { get; set; }
    public SocialPublicationStatus status { get; set; } = SocialPublicationStatus.Pending;
    public string? remote_post_id { get; set; }
    public string? remote_url { get; set; }
    public string? error_message { get; set; }
    public DateTime? published_at { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
}
