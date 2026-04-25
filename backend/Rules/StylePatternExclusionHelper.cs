using DocumentFormat.OpenXml.Wordprocessing;

namespace ThesisValidator.Rules;

internal static class StylePatternExclusionHelper
{
    // Style patterns to skip (case-insensitive matching)
    private static readonly string[] ExcludedStylePatterns =
    [
        "heading", "nagwek",           // Headings (EN/PL)
        "title", "tytu",               // Title (EN/PL)
        "subtitle", "podtytu",         // Subtitle (EN/PL)
        "caption", "podpis",           // Caption (EN/PL)
        "toc", "spis",                 // Table of Contents (EN/PL)
        "quote", "cytat",              // Quotes (EN/PL)
        "header", "footer",            // Header/Footer
        "list", "lista",               // List styles
        "legend", "legenda",
        "figure", "rysunek",
        "rys"
    ];

    public static bool HasExcludedStyle(Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return false;

        var styleLower = styleId.ToLowerInvariant();
        return ExcludedStylePatterns.Any(pattern => styleLower.Contains(pattern));
    }
}
