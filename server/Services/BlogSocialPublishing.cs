using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using server.Domain;
using server.Utils;
using static server.Services.BlogSocialPublishingHelpers;

namespace server.Services;

public sealed record SocialPublishImage(
    Guid asset_id,
    string file_name,
    string content_type,
    string alt_text,
    byte[] content,
    string public_url
);

public sealed record SocialPublishRequest(
    Guid post_id,
    string slug,
    string title,
    string excerpt,
    string body_html,
    string body_text,
    string public_post_url,
    IReadOnlyList<SocialPublishImage> images
);

public sealed record SocialPublishResult(
    SocialChannel channel,
    SocialPublicationStatus status,
    string? remote_post_id,
    string? remote_url,
    string? error_message)
{
    public static SocialPublishResult Published(SocialChannel channel, string? remotePostId, string? remoteUrl)
        => new(channel, SocialPublicationStatus.Published, remotePostId, remoteUrl, null);

    public static SocialPublishResult Failed(SocialChannel channel, string errorMessage)
        => new(channel, SocialPublicationStatus.Failed, null, null, errorMessage);
}

public interface ISocialPostPublisher
{
    SocialChannel Channel { get; }
    Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken);
}

public interface IBlogSocialPublisher
{
    Task<IReadOnlyList<SocialPublishResult>> PublishAllAsync(SocialPublishRequest request, CancellationToken cancellationToken);
    Task<SocialPublishResult> PublishAsync(SocialChannel channel, SocialPublishRequest request, CancellationToken cancellationToken);
}

public sealed class CompositeSocialPostPublisher : IBlogSocialPublisher
{
    private readonly IReadOnlyDictionary<SocialChannel, ISocialPostPublisher> _publishers;

    public CompositeSocialPostPublisher(IEnumerable<ISocialPostPublisher> publishers)
    {
        _publishers = publishers.ToDictionary(publisher => publisher.Channel);
    }

    public async Task<IReadOnlyList<SocialPublishResult>> PublishAllAsync(SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var results = new List<SocialPublishResult>();

        foreach (var channel in Enum.GetValues<SocialChannel>().OrderBy(channel => (int)channel))
            results.Add(await PublishAsync(channel, request, cancellationToken));

        return results;
    }

    public async Task<SocialPublishResult> PublishAsync(SocialChannel channel, SocialPublishRequest request, CancellationToken cancellationToken)
    {
        if (!_publishers.TryGetValue(channel, out var publisher))
            return SocialPublishResult.Failed(channel, "Canal social não configurado no servidor.");

        return await publisher.PublishAsync(request, cancellationToken);
    }
}

public sealed class FacebookPostPublisher : ISocialPostPublisher
{
    private readonly IHttpClientFactory _httpClientFactory;
    public SocialChannel Channel => SocialChannel.Facebook;

    public FacebookPostPublisher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var pageId = (Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ID") ?? string.Empty).Trim();
        var accessToken = (Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ACCESS_TOKEN") ?? string.Empty).Trim();
        var graphVersion = (Environment.GetEnvironmentVariable("META_GRAPH_API_VERSION") ?? "v23.0").Trim();

        if (string.IsNullOrWhiteSpace(pageId) || string.IsNullOrWhiteSpace(accessToken))
            return SocialPublishResult.Failed(Channel, "Integração do Facebook não configurada.");

        var client = _httpClientFactory.CreateClient();
        var attachedMedia = new List<string>();

        try
        {
            foreach (var image in request.images)
            {
                var photoResponse = await client.PostAsync(
                    $"https://graph.facebook.com/{graphVersion}/{pageId}/photos",
                    new FormUrlEncodedContent(
                    [
                        new("url", image.public_url),
                        new("published", "false"),
                        new("access_token", accessToken),
                    ]),
                    cancellationToken);

                var photoPayload = await photoResponse.Content.ReadAsStringAsync(cancellationToken);
                if (!photoResponse.IsSuccessStatusCode)
                    return SocialPublishResult.Failed(Channel, ExtractErrorMessage(photoPayload) ?? "Falha ao enviar mídia para o Facebook.");

                using var photoJson = JsonDocument.Parse(photoPayload);
                if (!photoJson.RootElement.TryGetProperty("id", out var mediaIdElement))
                    return SocialPublishResult.Failed(Channel, "Facebook não retornou o ID da mídia enviada.");

                attachedMedia.Add(mediaIdElement.GetString() ?? string.Empty);
            }

            var postFields = new List<KeyValuePair<string, string>>
            {
                new("message", BuildSocialText(request)),
                new("access_token", accessToken),
            };

            for (var index = 0; index < attachedMedia.Count; index++)
                postFields.Add(new($"attached_media[{index}]", JsonSerializer.Serialize(new { media_fbid = attachedMedia[index] })));

            var postResponse = await client.PostAsync(
                $"https://graph.facebook.com/{graphVersion}/{pageId}/feed",
                new FormUrlEncodedContent(postFields),
                cancellationToken);

            var postPayload = await postResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!postResponse.IsSuccessStatusCode)
                return SocialPublishResult.Failed(Channel, ExtractErrorMessage(postPayload) ?? "Falha ao publicar no Facebook.");

            using var postJson = JsonDocument.Parse(postPayload);
            var remoteId = postJson.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            return SocialPublishResult.Published(Channel, remoteId, BuildFacebookPostUrl(pageId, remoteId));
        }
        catch (Exception ex)
        {
            return SocialPublishResult.Failed(Channel, $"Falha ao publicar no Facebook: {ex.Message}");
        }
    }

    private static string? BuildFacebookPostUrl(string pageId, string? remotePostId)
    {
        if (string.IsNullOrWhiteSpace(remotePostId))
            return null;

        var parts = remotePostId.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
            return $"https://www.facebook.com/{pageId}/posts/{parts[1]}";

        return $"https://www.facebook.com/{pageId}";
    }
}

public sealed class InstagramPostPublisher : ISocialPostPublisher
{
    private readonly IHttpClientFactory _httpClientFactory;
    public SocialChannel Channel => SocialChannel.Instagram;

    public InstagramPostPublisher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var businessAccountId = (Environment.GetEnvironmentVariable("INSTAGRAM_BUSINESS_ACCOUNT_ID") ?? string.Empty).Trim();
        var accessToken = (Environment.GetEnvironmentVariable("INSTAGRAM_ACCESS_TOKEN") ?? string.Empty).Trim();
        var graphVersion = (Environment.GetEnvironmentVariable("META_GRAPH_API_VERSION") ?? "v23.0").Trim();

        if (string.IsNullOrWhiteSpace(businessAccountId) || string.IsNullOrWhiteSpace(accessToken))
            return SocialPublishResult.Failed(Channel, "Integração do Instagram não configurada.");

        var image = request.images.FirstOrDefault();
        if (image is null)
            return SocialPublishResult.Failed(Channel, "Instagram exige pelo menos uma imagem para publicar.");

        var client = _httpClientFactory.CreateClient();

        try
        {
            var createContainerResponse = await client.PostAsync(
                $"https://graph.facebook.com/{graphVersion}/{businessAccountId}/media",
                new FormUrlEncodedContent(
                [
                    new("image_url", image.public_url),
                    new("caption", BuildSocialText(request, 2200)),
                    new("access_token", accessToken),
                ]),
                cancellationToken);

            var containerPayload = await createContainerResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!createContainerResponse.IsSuccessStatusCode)
                return SocialPublishResult.Failed(Channel, ExtractErrorMessage(containerPayload) ?? "Falha ao criar mídia no Instagram.");

            using var containerJson = JsonDocument.Parse(containerPayload);
            if (!containerJson.RootElement.TryGetProperty("id", out var containerIdElement))
                return SocialPublishResult.Failed(Channel, "Instagram não retornou o ID do container de mídia.");

            var containerId = containerIdElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(containerId))
                return SocialPublishResult.Failed(Channel, "Instagram retornou um ID de container inválido.");

            var publishResponse = await client.PostAsync(
                $"https://graph.facebook.com/{graphVersion}/{businessAccountId}/media_publish",
                new FormUrlEncodedContent(
                [
                    new("creation_id", containerId),
                    new("access_token", accessToken),
                ]),
                cancellationToken);

            var publishPayload = await publishResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!publishResponse.IsSuccessStatusCode)
                return SocialPublishResult.Failed(Channel, ExtractErrorMessage(publishPayload) ?? "Falha ao publicar no Instagram.");

            using var publishJson = JsonDocument.Parse(publishPayload);
            var remoteId = publishJson.RootElement.TryGetProperty("id", out var mediaIdElement) ? mediaIdElement.GetString() : null;
            return SocialPublishResult.Published(Channel, remoteId, null);
        }
        catch (Exception ex)
        {
            return SocialPublishResult.Failed(Channel, $"Falha ao publicar no Instagram: {ex.Message}");
        }
    }
}

public sealed class LinkedInPostPublisher : ISocialPostPublisher
{
    private readonly IHttpClientFactory _httpClientFactory;
    public SocialChannel Channel => SocialChannel.LinkedIn;

    public LinkedInPostPublisher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var organizationId = (Environment.GetEnvironmentVariable("LINKEDIN_ORGANIZATION_ID") ?? string.Empty).Trim();
        var accessToken = (Environment.GetEnvironmentVariable("LINKEDIN_ACCESS_TOKEN") ?? string.Empty).Trim();
        var apiVersion = (Environment.GetEnvironmentVariable("LINKEDIN_API_VERSION") ?? "202504").Trim();

        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(accessToken))
            return SocialPublishResult.Failed(Channel, "Integração do LinkedIn não configurada.");

        var authorUrn = $"urn:li:organization:{organizationId}";
        var client = _httpClientFactory.CreateClient();

        try
        {
            string? imageUrn = null;
            var firstImage = request.images.FirstOrDefault();
            if (firstImage is not null)
                imageUrn = await UploadLinkedInImageAsync(client, authorUrn, accessToken, apiVersion, firstImage, cancellationToken);

            var payload = BuildLinkedInPostPayload(authorUrn, request, imageUrn);
            using var postRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts");
            postRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            postRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
            postRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var postResponse = await client.SendAsync(postRequest, cancellationToken);
            var postBody = await postResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!postResponse.IsSuccessStatusCode)
                return SocialPublishResult.Failed(Channel, ExtractErrorMessage(postBody) ?? "Falha ao publicar no LinkedIn.");

            var remoteId = postResponse.Headers.TryGetValues("x-restli-id", out var values)
                ? values.FirstOrDefault()
                : null;

            return SocialPublishResult.Published(Channel, remoteId, BuildLinkedInPostUrl(remoteId));
        }
        catch (Exception ex)
        {
            return SocialPublishResult.Failed(Channel, $"Falha ao publicar no LinkedIn: {ex.Message}");
        }
    }

    private static async Task<string> UploadLinkedInImageAsync(
        HttpClient client,
        string authorUrn,
        string accessToken,
        string apiVersion,
        SocialPublishImage image,
        CancellationToken cancellationToken)
    {
        using var initRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/rest/images?action=initializeUpload");
        initRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        initRequest.Headers.Add("LinkedIn-Version", apiVersion);
        initRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        initRequest.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                initializeUploadRequest = new
                {
                    owner = authorUrn
                }
            }),
            Encoding.UTF8,
            "application/json");

        var initResponse = await client.SendAsync(initRequest, cancellationToken);
        var initPayload = await initResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!initResponse.IsSuccessStatusCode)
            throw new InvalidOperationException(ExtractErrorMessage(initPayload) ?? "Falha ao inicializar upload de imagem no LinkedIn.");

        using var initJson = JsonDocument.Parse(initPayload);
        var valueElement = initJson.RootElement.GetProperty("value");
        var uploadUrl = valueElement.GetProperty("uploadUrl").GetString();
        var imageUrn = valueElement.GetProperty("image").GetString();

        if (string.IsNullOrWhiteSpace(uploadUrl) || string.IsNullOrWhiteSpace(imageUrn))
            throw new InvalidOperationException("LinkedIn não retornou os dados necessários para upload da imagem.");

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        uploadRequest.Content = new ByteArrayContent(image.content);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(image.content_type);

        var uploadResponse = await client.SendAsync(uploadRequest, cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            var uploadPayload = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(ExtractErrorMessage(uploadPayload) ?? "Falha ao enviar a imagem ao LinkedIn.");
        }

        return imageUrn;
    }

    private static string BuildLinkedInPostPayload(string authorUrn, SocialPublishRequest request, string? imageUrn)
    {
        var body = imageUrn is null
            ? new
            {
                author = authorUrn,
                lifecycleState = "PUBLISHED",
                specificContent = new Dictionary<string, object>
                {
                    ["com.linkedin.ugc.ShareContent"] = new
                    {
                        shareCommentary = new
                        {
                            text = BuildSocialText(request, 3000)
                        },
                        shareMediaCategory = "NONE",
                        media = Array.Empty<object>()
                    }
                },
                visibility = new Dictionary<string, string>
                {
                    ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
                }
            }
            : new
            {
                author = authorUrn,
                lifecycleState = "PUBLISHED",
                specificContent = new Dictionary<string, object>
                {
                    ["com.linkedin.ugc.ShareContent"] = new
                    {
                        shareCommentary = new
                        {
                            text = BuildSocialText(request, 3000)
                        },
                        shareMediaCategory = "IMAGE",
                        media = new[]
                        {
                            new
                            {
                                status = "READY",
                                media = imageUrn,
                                title = new
                                {
                                    text = request.title
                                }
                            }
                        }
                    }
                },
                visibility = new Dictionary<string, string>
                {
                    ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
                }
            };

        return JsonSerializer.Serialize(body);
    }

    private static string? BuildLinkedInPostUrl(string? remoteId)
    {
        if (string.IsNullOrWhiteSpace(remoteId))
            return null;

        return $"https://www.linkedin.com/feed/update/{Uri.EscapeDataString(remoteId)}";
    }
}

internal static class BlogSocialPublishingHelpers
{
    public static string BuildSocialText(SocialPublishRequest request, int maxLength = 4000)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.title))
            parts.Add(request.title.Trim());

        if (!string.IsNullOrWhiteSpace(request.excerpt))
            parts.Add(request.excerpt.Trim());
        else if (!string.IsNullOrWhiteSpace(request.body_text))
            parts.Add(request.body_text.Trim());

        if (!string.IsNullOrWhiteSpace(request.public_post_url))
            parts.Add(request.public_post_url.Trim());

        var combined = string.Join("\n\n", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        if (combined.Length <= maxLength)
            return combined;

        return combined[..Math.Max(0, maxLength - 1)].TrimEnd() + "…";
    }

    public static string? ExtractErrorMessage(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.ValueKind == JsonValueKind.Object &&
                    errorElement.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString();
                }

                return errorElement.ToString();
            }

            if (document.RootElement.TryGetProperty("message", out var rootMessage))
                return rootMessage.GetString();
        }
        catch
        {
            return payload.Length > 240 ? payload[..240] : payload;
        }

        return payload.Length > 240 ? payload[..240] : payload;
    }
}
