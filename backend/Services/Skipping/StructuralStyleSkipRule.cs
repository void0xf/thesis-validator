using backend.Services.Formatting;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Skipping;

public sealed class StructuralStyleSkipRule : ISkipRule
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

    public SkipDecision ShouldSkipParagraph(
        WordprocessingDocument? doc,
        Paragraph paragraph,
        UniversityConfig config,
        SkipContext context)
    {
        return ShouldSkipParagraph(
            doc,
            paragraph,
            config,
            context,
            excludeListStyles: true);
    }

    public SkipDecision ShouldSkipParagraph(
        WordprocessingDocument? doc,
        Paragraph paragraph,
        UniversityConfig config,
        SkipContext context,
        bool excludeListStyles)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
        if (ContainsExcludedPattern(styleId, excludeListStyles))
        {
            return SkipDecision.Skip(
                SkipReason.StructuralStyle,
                $"Paragraph style '{styleId}' is excluded from body-text validation.");
        }

        if (doc is null)
            return SkipDecision.Include;

        var style = StyleResolutionService.FindStyle(doc, styleId);
        var styleName = style?.StyleName?.Val?.Value;
        if (ContainsExcludedPattern(styleName, excludeListStyles))
        {
            return SkipDecision.Skip(
                SkipReason.StructuralStyle,
                $"Paragraph style name '{styleName}' is excluded from body-text validation.");
        }

        var outlineLevel = style?.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
        return outlineLevel.HasValue && outlineLevel.Value <= 8
            ? SkipDecision.Skip(SkipReason.StructuralStyle, "Paragraph style has a heading outline level.")
            : SkipDecision.Include;
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
