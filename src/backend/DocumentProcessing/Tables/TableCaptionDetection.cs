using System.Text.RegularExpressions;
using backend.DocumentProcessing.Content;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.Tables;

public static class TableCaptionDetection
{
    private static readonly Regex TableCaptionStartRegex = new(
        @"^\s*(?:Tabela\b|Tab\.|Table\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsRealTable(Table table)
    {
        return table.Elements<TableRow>()
            .Any(row => row.Elements<TableCell>().Any());
    }

    public static bool LooksLikeTableCaption(Paragraph paragraph, bool skipTextBoxes = true)
    {
        var text = TextExtractor.GetParagraphText(paragraph, skipTextBoxes);
        return LooksLikeTableCaptionText(text);
    }

    public static bool LooksLikeTableCaptionText(string? text)
    {
        return !string.IsNullOrWhiteSpace(text)
            && TableCaptionStartRegex.IsMatch(text);
    }

    public static bool HasCaptionImmediatelyAbove(
        OpenXmlElementList children,
        int tableChildIndex)
    {
        return GetPreviousBodyParagraph(children, tableChildIndex) is { } captionAbove
            && LooksLikeTableCaption(captionAbove);
    }

    public static bool HasCaptionImmediatelyBelow(
        OpenXmlElementList children,
        int tableChildIndex)
    {
        return GetNextBodyParagraph(children, tableChildIndex) is { } captionBelow
            && LooksLikeTableCaption(captionBelow);
    }

    public static Paragraph? GetNextBodyParagraph(
        OpenXmlElementList children,
        int tableChildIndex)
    {
        return tableChildIndex + 1 < children.Count
            ? children[tableChildIndex + 1] as Paragraph
            : null;
    }

    private static Paragraph? GetPreviousBodyParagraph(
        OpenXmlElementList children,
        int tableChildIndex)
    {
        return tableChildIndex > 0
            ? children[tableChildIndex - 1] as Paragraph
            : null;
    }
}
