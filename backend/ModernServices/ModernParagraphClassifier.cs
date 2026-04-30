using backend.ModernServices.Formatting;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.ModernServices;

public sealed class ModernParagraphClassifier
{
    private static readonly string[] ExcludedStylePatterns =
    [
        "heading", "nagwek",
        "title", "tytu",
        "subtitle", "podtytu",
        "caption", "podpis",
        "toc", "spis",
        "quote", "cytat",
        "header", "footer",
        "list", "lista",
        "legend", "legenda",
        "figure", "rysunek",
        "rys",
        "table",
        "bibliography"
    ];

    public bool IsListItem(Paragraph paragraph)
    {
        return paragraph.ParagraphProperties?.NumberingProperties is not null;
    }

    public bool HasExcludedStructuralStyle(
        WordprocessingDocument document,
        Paragraph paragraph,
        bool excludeListStyles = true)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
        if (ContainsExcludedPattern(styleId, excludeListStyles))
            return true;

        var style = StyleResolutionService.FindStyle(document, styleId);
        var styleName = style?.StyleName?.Val?.Value;
        if (ContainsExcludedPattern(styleName, excludeListStyles))
            return true;

        var outlineLevel = style?.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
        return outlineLevel.HasValue && outlineLevel.Value <= 8;
    }

    private static bool ContainsExcludedPattern(string? value, bool excludeListStyles)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var valueLower = value.ToLowerInvariant();
        return ExcludedStylePatterns.Any(pattern =>
            (excludeListStyles || !IsListStylePattern(pattern))
            && valueLower.Contains(pattern));
    }

    private static bool IsListStylePattern(string pattern)
    {
        return pattern is "list" or "lista";
    }
}
