using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Formatting;

public static class StyleResolutionService
{
    public static string? GetParagraphStyleId(Paragraph paragraph)
    {
        return paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
    }

    public static Style? FindStyle(WordprocessingDocument doc, string? styleId)
    {
        if (string.IsNullOrEmpty(styleId))
            return null;

        return doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .Elements<Style>()
            .FirstOrDefault(style =>
                string.Equals(style.StyleId?.Value, styleId, StringComparison.OrdinalIgnoreCase));
    }

    public static string? GetStyleName(WordprocessingDocument doc, string? styleId)
    {
        return FindStyle(doc, styleId)?.StyleName?.Val?.Value;
    }

    public static IEnumerable<Style> GetStyleChain(WordprocessingDocument doc, string? styleId)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentStyleId = styleId;

        while (!string.IsNullOrEmpty(currentStyleId) && visited.Add(currentStyleId))
        {
            var style = FindStyle(doc, currentStyleId);
            if (style is null)
                yield break;

            yield return style;
            currentStyleId = style.BasedOn?.Val?.Value;
        }
    }

    public static Style? GetDefaultParagraphStyle(WordprocessingDocument doc)
    {
        return doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .Elements<Style>()
            .FirstOrDefault(style =>
                style.Type?.Value == StyleValues.Paragraph &&
                style.Default?.Value == true);
    }

    public static ParagraphPropertiesBaseStyle? GetDocumentDefaultParagraphProperties(
        WordprocessingDocument doc)
    {
        return doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .DocDefaults?
            .ParagraphPropertiesDefault?
            .ParagraphPropertiesBaseStyle;
    }

    public static RunPropertiesBaseStyle? GetDocumentDefaultRunProperties(
        WordprocessingDocument doc)
    {
        return doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .DocDefaults?
            .RunPropertiesDefault?
            .RunPropertiesBaseStyle;
    }
}
