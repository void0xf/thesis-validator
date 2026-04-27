using System.Globalization;
using System.Text;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using backend.Services.Skipping;

namespace backend.Services.Extraction;

public static class TextExtractionService
{
    public static string GetParagraphText(
        WordprocessingDocument doc,
        Paragraph paragraph,
        UniversityConfig config)
    {
        return GetParagraphText(paragraph, config);
    }

    public static string GetParagraphText(Paragraph paragraph, UniversityConfig config)
    {
        return GetParagraphText(paragraph, SkipDecisionService.ShouldSkipTextBoxes(config));
    }

    public static string GetParagraphText(Paragraph paragraph, bool skipTextBoxes)
    {
        var textElements = paragraph.Descendants<Text>();
        if (skipTextBoxes)
        {
            textElements = textElements.Where(text => !TextBoxSkipRule.IsInsideTextBoxOrDrawingText(text));
        }

        return string.Concat(textElements.Select(t => t.Text));
    }

    public static string GetRunText(Run run, UniversityConfig config)
    {
        var textElements = run.Elements<Text>();
        if (SkipDecisionService.ShouldSkipTextBoxes(config))
        {
            textElements = textElements.Where(text => !TextBoxSkipRule.IsInsideTextBoxOrDrawingText(text));
        }

        return string.Concat(textElements.Select(t => t.Text));
    }

    public static bool HasMeaningfulParagraphContent(Paragraph paragraph, UniversityConfig config)
    {
        return HasMeaningfulContent(GetParagraphText(paragraph, config));
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

    public static string GetPreview(Paragraph paragraph, UniversityConfig config, int maxLength)
    {
        return Truncate(SanitizePreview(GetParagraphText(paragraph, config)), maxLength);
    }

    public static string GetPreview(WordprocessingDocument doc, Paragraph paragraph, UniversityConfig config, int maxLength)
    {
        return Truncate(SanitizePreview(GetParagraphText(doc, paragraph, config)), maxLength);
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
