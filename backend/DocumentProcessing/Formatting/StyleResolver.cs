using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.Formatting;

public static class StyleResolver
{
    public static string? GetParagraphStyleId(Paragraph paragraph)
    {
        return paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
    }

    public static Style? FindStyle(WordprocessingDocument document, string? styleId)
    {
        if (string.IsNullOrEmpty(styleId))
            return null;

        return document.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .Elements<Style>()
            .FirstOrDefault(style =>
                string.Equals(style.StyleId?.Value, styleId, StringComparison.OrdinalIgnoreCase));
    }

    public static string? GetStyleName(WordprocessingDocument document, string? styleId)
    {
        return FindStyle(document, styleId)?.StyleName?.Val?.Value;
    }

    public static IEnumerable<Style> GetStyleChain(WordprocessingDocument document, string? styleId)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentStyleId = styleId;

        while (!string.IsNullOrEmpty(currentStyleId) && visited.Add(currentStyleId))
        {
            var style = FindStyle(document, currentStyleId);
            if (style is null)
                yield break;

            yield return style;
            currentStyleId = style.BasedOn?.Val?.Value;
        }
    }

    public static Style? GetDefaultParagraphStyle(WordprocessingDocument document)
    {
        return document.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .Elements<Style>()
            .FirstOrDefault(style =>
                style.Type?.Value == StyleValues.Paragraph
                && style.Default?.Value == true);
    }

    public static ParagraphPropertiesBaseStyle? GetDocumentDefaultParagraphProperties(
        WordprocessingDocument document)
    {
        return document.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .DocDefaults?
            .ParagraphPropertiesDefault?
            .ParagraphPropertiesBaseStyle;
    }

    public static RunPropertiesBaseStyle? GetDocumentDefaultRunProperties(
        WordprocessingDocument document)
    {
        return document.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .DocDefaults?
            .RunPropertiesDefault?
            .RunPropertiesBaseStyle;
    }
}
