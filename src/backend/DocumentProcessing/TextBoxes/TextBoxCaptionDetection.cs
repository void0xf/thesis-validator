using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Tables;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace backend.DocumentProcessing.TextBoxes;

public static class TextBoxCaptionDetection
{
    private static readonly Regex TextBoxCaptionRegex = new(
        @"^\s*(?:Tekst(?:\.|owy)?|Text(?:\s*box)?|Textbox|Ramka(?:\s+tekstowa)?)\s*:?\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool HasAdjacentCaption(
        WordprocessingDocument document,
        OpenXmlElementList children,
        int textBoxParagraphChildIndex)
    {
        return IsCaptionParagraph(document, GetPreviousBodyParagraph(children, textBoxParagraphChildIndex))
            || IsCaptionParagraph(document, GetNextBodyParagraph(children, textBoxParagraphChildIndex));
    }

    public static Paragraph? GetNextBodyParagraph(
        OpenXmlElementList children,
        int paragraphChildIndex)
    {
        return paragraphChildIndex + 1 < children.Count
            ? children[paragraphChildIndex + 1] as Paragraph
            : null;
    }

    private static bool IsCaptionParagraph(
        WordprocessingDocument document,
        Paragraph? paragraph)
    {
        if (paragraph is null)
            return false;

        var text = TextExtractor.GetParagraphText(paragraph, skipTextBoxes: true);
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return LooksLikeTextBoxCaptionText(text)
            || CaptionDetection.LooksLikeFigureCaptionText(text)
            || TableCaptionDetection.LooksLikeTableCaptionText(text)
            || CaptionDetection.UsesDedicatedCaptionStyle(document, paragraph);
    }

    public static bool LooksLikeTextBoxCaptionText(string? text)
    {
        return !string.IsNullOrWhiteSpace(text)
            && TextBoxCaptionRegex.IsMatch(text);
    }

    private static Paragraph? GetPreviousBodyParagraph(
        OpenXmlElementList children,
        int paragraphChildIndex)
    {
        return paragraphChildIndex > 0
            ? children[paragraphChildIndex - 1] as Paragraph
            : null;
    }
}
