using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace server.Utils;

public static partial class BlogSlug
{
    public static string Create(string? title)
    {
        var input = (title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(input))
            return "post";

        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        var ascii = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        ascii = NonSlugCharsRegex().Replace(ascii, "-");
        ascii = MultipleDashesRegex().Replace(ascii, "-").Trim('-');

        return string.IsNullOrWhiteSpace(ascii) ? "post" : ascii;
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonSlugCharsRegex();

    [GeneratedRegex(@"\-+", RegexOptions.Compiled)]
    private static partial Regex MultipleDashesRegex();
}
