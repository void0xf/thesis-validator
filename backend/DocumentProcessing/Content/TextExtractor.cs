using System.Globalization;
using System.Text;
using backend.DocumentProcessing.Skipping;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.Content;

public static class TextExtractor
{
    public static string GetParagraphText(Paragraph paragraph, bool skipTextBoxes)
    {
        var textElements = paragraph.Descendants<Text>();
        if (skipTextBoxes)
        {
            textElements = textElements.Where(text => !TextBoxContentDetector.IsInsideTextBoxOrDrawingText(text));
        }

        return string.Concat(textElements.Select(text => text.Text));
    }

    public static string GetParagraphText(
        WordprocessingDocument document,
        Paragraph paragraph,
        bool skipTextBoxes)
    {
        return GetParagraphText(paragraph, skipTextBoxes);
    }

    public static string GetRunText(Run run, bool skipTextBoxes = true)
    {
        var textElements = run.Elements<Text>();
        if (skipTextBoxes)
        {
            textElements = textElements.Where(text => !TextBoxContentDetector.IsInsideTextBoxOrDrawingText(text));
        }

        return string.Concat(textElements.Select(text => text.Text));
    }

    public static bool HasMeaningfulContent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
                continue;

            if (char.GetUnicodeCategory(ch) == UnicodeCategory.Format)
                continue;

            return true;
        }

        return false;
    }

    public static string GetPreview(Paragraph paragraph, bool skipTextBoxes, int maxLength)
    {
        return Truncate(SanitizePreview(GetParagraphText(paragraph, skipTextBoxes)), maxLength);
    }

    public static string GetPreview(
        WordprocessingDocument document,
        Paragraph paragraph,
        bool skipTextBoxes,
        int maxLength)
    {
        return Truncate(SanitizePreview(GetParagraphText(document, paragraph, skipTextBoxes)), maxLength);
    }

    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }

    public static string SanitizePreview(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch == '\u00A0')
            {
                builder.Append(' ');
                continue;
            }

            if (char.IsControl(ch))
                continue;

            if (char.GetUnicodeCategory(ch) == UnicodeCategory.Format)
                continue;

            builder.Append(ch);
        }

        return builder.ToString().Trim();
    }
}
