using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services;

/// <summary>
/// Centralised heading-style detection that works for any Word locale
/// (English "Heading 1", Polish "Nagłówek 1", etc.).
///
/// Resolution order:
///   1. Style ID pattern match (heading1, Nagwek1, …)
///   2. Outline level defined on the paragraph style (most reliable, locale-independent)
/// </summary>
public static class HeadingStyleHelper
{
    /// <summary>
    /// Style ID substrings that indicate non-heading styles which may still
    /// carry an outline level (e.g. "TOC Heading" has outlineLvl 9).
    /// </summary>
    private static readonly string[] NonHeadingPatterns =
    [
        "toc",                  // TOC1-9, TOCHeading, Spistreci
        "tableofcontents",
        "spistreci",            // Polish TOC
        "caption", "podpis",
        "title", "tytu",
        "subtitle", "podtytu",
        "header", "footer",
        "footnote", "endnote",
        "listparagraph"
    ];

    /// <summary>
    /// Returns the 1-based heading level for a paragraph, or <c>null</c> if the
    /// paragraph does not use a heading style.
    /// </summary>
    public static int? GetHeadingLevel(WordprocessingDocument doc, Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return null;

        // Reject known non-heading styles early (TOC, caption, etc.).
        if (IsNonHeadingStyle(styleId))
            return null;

        // Fast path: try to extract the level from the style ID itself.
        var levelFromId = ParseLevelFromStyleId(styleId);
        if (levelFromId is not null)
            return levelFromId;

        // Fallback: look at the outline level defined in the style (locale-independent).
        return GetOutlineLevelFromStyle(doc, styleId);
    }

    private static bool IsNonHeadingStyle(string styleId)
    {
        var lower = styleId.ToLowerInvariant();
        return NonHeadingPatterns.Any(lower.Contains);
    }

    /// <summary>
    /// Returns <c>true</c> when the paragraph uses any heading style.
    /// </summary>
    public static bool IsHeading(WordprocessingDocument doc, Paragraph paragraph)
    {
        return GetHeadingLevel(doc, paragraph) is not null;
    }

    // ------------------------------------------------------------------ //
    //  Pattern matching on style ID
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Attempts to parse the heading level directly from the style ID string.
    /// Handles English ("Heading1", "heading2"), Polish ("Nagwek1", "Nagłówek2"),
    /// and other common variants.
    /// </summary>
    private static int? ParseLevelFromStyleId(string styleId)
    {
        // Walk backwards from the end to collect trailing digits.
        int digitStart = styleId.Length;
        while (digitStart > 0 && char.IsAsciiDigit(styleId[digitStart - 1]))
            digitStart--;

        if (digitStart == styleId.Length)
            return null;                       // no trailing digits

        var prefix = styleId[..digitStart].TrimEnd();

        if (!IsKnownHeadingPrefix(prefix))
            return null;

        if (int.TryParse(styleId[digitStart..], out var level) && level >= 1)
            return level;

        return null;
    }

    private static bool IsKnownHeadingPrefix(string prefix)
    {
        // Compare lowercase, ignore diacritics for robustness.
        var lower = prefix.ToLowerInvariant();

        return lower is
            "heading"   or   // English
            "nagwek"    or   // Polish (ASCII style ID)
            "nagłówek"  or   // Polish (with diacritics)
            "nag\u0142\u00f3wek" or // Polish (explicit Unicode, same as above)
            "überschrift" or // German
            "titre"     or   // French
            "título"    or   // Spanish
            "titolo";        // Italian
    }

    // ------------------------------------------------------------------ //
    //  Outline-level fallback (works for every locale)
    // ------------------------------------------------------------------ //

    private static int? GetOutlineLevelFromStyle(WordprocessingDocument doc, string styleId)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        if (styles is null) return null;

        var style = styles.Elements<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId, styleId, StringComparison.OrdinalIgnoreCase));

        if (style is null) return null;

        // outlineLvl is 0-based in the XML: 0 = Heading 1 … 8 = Heading 9.
        // Value 9 means "Body Text" — it is NOT a heading level.
        var outlineLvl = style.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
        if (outlineLvl is not null && outlineLvl.Value <= 8)
            return outlineLvl.Value + 1;    // convert to 1-based

        // Check the parent (basedOn) style chain.
        var basedOn = style.BasedOn?.Val?.Value;
        if (!string.IsNullOrEmpty(basedOn) && !string.Equals(basedOn, styleId, StringComparison.OrdinalIgnoreCase))
            return GetOutlineLevelFromStyle(doc, basedOn);

        return null;
    }
}
