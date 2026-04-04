using server.Domain;

namespace server.Utils;

public static class InvestmentTypeDetector
{
    private static readonly (InvestmentType Type, string[][] Terms)[] Rules =
    [
        (InvestmentType.CDB, [["cdb"]]),
        (InvestmentType.LCI, [["lci"]]),
        (InvestmentType.LCA, [["lca"]]),
        (InvestmentType.RCI, [["rci"]]),
        (InvestmentType.RCA, [["rca"]]),
        (InvestmentType.Tesouro, [["tesouro"]]),
        (InvestmentType.Debentures, [["debentures"], ["debenture"]]),
        (InvestmentType.TitulosPublicos, [["titulos", "publicos"], ["titulo", "publico"], ["titulos"], ["titulo"]]),
        (InvestmentType.CRI, [["cri"]]),
        (InvestmentType.CRA, [["cra"]]),
    ];

    public static InvestmentType? Detect(string? title)
    {
        var words = SplitWords(title);
        if (words.Count == 0)
            return null;

        foreach (var rule in Rules)
        {
            if (rule.Terms.Any(term => WordsMatch(words, term)))
                return rule.Type;
        }

        return null;
    }

    private static List<string> SplitWords(string? value)
    {
        return Normalize(value)
            .Split(new[] { ' ', '\t', '\r', '\n', '-', '_', '/', '\\', '.', ',', ';', ':', '(', ')', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static string Normalize(string? value)
    {
        return string.Concat((value ?? string.Empty)
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();
    }

    private static bool WordsMatch(List<string> words, IReadOnlyList<string> candidateWords)
    {
        if (candidateWords.Count == 1)
            return words.Any(word => CompareWord(word, candidateWords[0]));

        for (var index = 0; index <= words.Count - candidateWords.Count; index += 1)
        {
            var matches = true;
            for (var offset = 0; offset < candidateWords.Count; offset += 1)
            {
                if (!CompareWord(words[index + offset], candidateWords[offset]))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return true;
        }

        return false;
    }

    private static bool CompareWord(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
