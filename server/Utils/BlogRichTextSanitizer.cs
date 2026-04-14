using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace server.Utils;

public static partial class BlogRichTextSanitizer
{
    private static readonly HashSet<string> InlineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "strong", "b", "em", "i", "u", "s", "strike"
    };

    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "ul", "ol", "li", "blockquote", "div", "h1", "h2", "h3", "h4"
    };

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var input = ScriptOrStyleRegex().Replace(html, string.Empty);
        input = HtmlCommentRegex().Replace(input, string.Empty);

        var result = new StringBuilder();
        var matches = HtmlTagRegex().Matches(input);
        var cursor = 0;

        foreach (Match match in matches)
        {
            if (match.Index > cursor)
                result.Append(EncodeText(input[cursor..match.Index]));

            result.Append(SanitizeTag(match.Value));
            cursor = match.Index + match.Length;
        }

        if (cursor < input.Length)
            result.Append(EncodeText(input[cursor..]));

        return NormalizeWhitespace(result.ToString());
    }

    public static string ToPlainText(string? sanitizedHtml)
    {
        if (string.IsNullOrWhiteSpace(sanitizedHtml))
            return string.Empty;

        var text = sanitizedHtml;
        text = ImgRegex().Replace(text, " ");
        text = LineBreakRegex().Replace(text, "\n");
        text = BlockClosingRegex().Replace(text, "\n");
        text = HtmlTagRegex().Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]+\n", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"[ \t]{2,}", " ");
        return text.Trim();
    }

    public static IReadOnlyList<Guid> ExtractAssetIds(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var ids = new HashSet<Guid>();
        var matches = ImgSrcRegex().Matches(html);

        foreach (Match match in matches)
        {
            var src = WebUtility.HtmlDecode(match.Groups["src"].Value);
            var path = TryGetAssetPath(src);
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var assetMatch = AssetPathRegex().Match(path);
            if (assetMatch.Success && Guid.TryParse(assetMatch.Groups["id"].Value, out var assetId))
                ids.Add(assetId);
        }

        return ids.ToList();
    }

    private static string SanitizeTag(string rawTag)
    {
        var tag = rawTag.Trim();
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith('<') || !tag.EndsWith('>'))
            return string.Empty;

        var inner = tag[1..^1].Trim();
        if (string.IsNullOrWhiteSpace(inner))
            return string.Empty;

        var closing = inner.StartsWith('/');
        if (closing)
            inner = inner[1..].Trim();

        var tagName = inner.Split([' ', '\t', '\r', '\n', '/'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tagName))
            return string.Empty;

        tagName = tagName.ToLowerInvariant();

        if (InlineTags.Contains(tagName))
            return closing ? $"</{NormalizeInlineTag(tagName)}>" : $"<{NormalizeInlineTag(tagName)}>";

        if (BlockTags.Contains(tagName))
        {
            var normalizedTag = tagName == "div" ? "p" : tagName;
            if (normalizedTag == "br")
                return "<br>";

            return closing ? $"</{normalizedTag}>" : $"<{normalizedTag}>";
        }

        if (tagName == "a")
        {
            if (closing)
                return "</a>";

            var href = ExtractHref(inner);
            return string.IsNullOrWhiteSpace(href)
                ? string.Empty
                : $"<a href=\"{WebUtility.HtmlEncode(href)}\" target=\"_blank\" rel=\"noopener noreferrer nofollow\">";
        }

        if (tagName == "img" && !closing)
        {
            var src = ExtractAttribute(inner, "src");
            var path = TryGetAssetPath(src);
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var assetMatch = AssetPathRegex().Match(path);
            if (!assetMatch.Success)
                return string.Empty;

            var alt = ExtractAttribute(inner, "alt") ?? string.Empty;
            return $"<img src=\"{WebUtility.HtmlEncode(path)}\" alt=\"{WebUtility.HtmlEncode(alt)}\">";
        }

        return string.Empty;
    }

    private static string NormalizeInlineTag(string tagName) => tagName switch
    {
        "b" => "strong",
        "i" => "em",
        "strike" => "s",
        _ => tagName
    };

    private static string EncodeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return WebUtility.HtmlEncode(text)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", "<br>", StringComparison.Ordinal);
    }

    private static string NormalizeWhitespace(string html)
    {
        var normalized = Regex.Replace(html, @"(<br>\s*){3,}", "<br><br>", RegexOptions.IgnoreCase);
        return normalized.Trim();
    }

    private static string? ExtractHref(string innerTag)
    {
        var href = ExtractAttribute(innerTag, "href")?.Trim();
        if (string.IsNullOrWhiteSpace(href))
            return null;

        if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return href;
        }

        return null;
    }

    private static string? TryGetAssetPath(string? src)
    {
        if (string.IsNullOrWhiteSpace(src))
            return null;

        if (Uri.TryCreate(src, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.AbsolutePath;

        if (Uri.TryCreate(src, UriKind.Relative, out var relativeUri))
            return relativeUri.ToString();

        return null;
    }

    private static string? ExtractAttribute(string innerTag, string attributeName)
    {
        var match = AttributeRegex().Matches(innerTag)
            .FirstOrDefault(item => item.Groups["name"].Value.Equals(attributeName, StringComparison.OrdinalIgnoreCase));

        return match?.Groups["value"].Value;
    }

    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStyleRegex();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlCommentRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"<(br)\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakRegex();

    [GeneratedRegex(@"</(p|li|blockquote|ul|ol|h1|h2|h3|h4)>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockClosingRegex();

    [GeneratedRegex(@"<(img)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgRegex();

    [GeneratedRegex(@"src\s*=\s*[""'](?<src>[^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ImgSrcRegex();

    [GeneratedRegex(@"/public/blog/assets/(?<id>[0-9a-fA-F\-]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex AssetPathRegex();

    [GeneratedRegex(@"(?<name>[a-zA-Z_:][a-zA-Z0-9_\:\-]*)\s*=\s*[""'](?<value>[^""']*)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex AttributeRegex();
}
