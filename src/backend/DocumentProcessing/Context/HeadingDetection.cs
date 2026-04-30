using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.Context;

public static class HeadingDetection
{
    private static readonly string[] NonHeadingPatterns =
    [
        "toc",
        "tableofcontents",
        "spistreci",
        "caption", "podpis",
        "title", "tytu",
        "subtitle", "podtytu",
        "header", "footer",
        "footnote", "endnote",
        "listparagraph"
    ];

    public static int? GetHeadingLevel(WordprocessingDocument document, Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return null;

        if (IsNonHeadingStyle(styleId))
            return null;

        var levelFromId = ParseLevelFromStyleId(styleId);
        return levelFromId ?? GetOutlineLevelFromStyle(document, styleId);
    }

    public static bool IsHeading(WordprocessingDocument document, Paragraph paragraph)
    {
        return GetHeadingLevel(document, paragraph) is not null;
    }

    private static bool IsNonHeadingStyle(string styleId)
    {
        var lower = styleId.ToLowerInvariant();
        return NonHeadingPatterns.Any(lower.Contains);
    }

    private static int? ParseLevelFromStyleId(string styleId)
    {
        var digitStart = styleId.Length;
        while (digitStart > 0 && char.IsAsciiDigit(styleId[digitStart - 1]))
        {
            digitStart--;
        }

        if (digitStart == styleId.Length)
            return null;

        var prefix = styleId[..digitStart].TrimEnd();
        if (!IsKnownHeadingPrefix(prefix))
            return null;

        return int.TryParse(styleId[digitStart..], out var level) && level >= 1
            ? level
            : null;
    }

    private static bool IsKnownHeadingPrefix(string prefix)
    {
        var lower = prefix.ToLowerInvariant();

        return lower is
            "heading" or
            "nagwek" or
            "nag\u0142\u00f3wek" or
            "\u00fcberschrift" or
            "titre" or
            "t\u00edtulo" or
            "titolo";
    }

    private static int? GetOutlineLevelFromStyle(WordprocessingDocument document, string styleId)
    {
        var styles = document.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        if (styles is null)
            return null;

        var style = styles.Elements<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId, styleId, StringComparison.OrdinalIgnoreCase));
        if (style is null)
            return null;

        var outlineLevel = style.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
        if (outlineLevel is not null && outlineLevel.Value <= 8)
            return outlineLevel.Value + 1;

        var basedOn = style.BasedOn?.Val?.Value;
        if (!string.IsNullOrEmpty(basedOn) && !string.Equals(basedOn, styleId, StringComparison.OrdinalIgnoreCase))
            return GetOutlineLevelFromStyle(document, basedOn);

        return null;
    }
}
