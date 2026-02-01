using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace Rules;

/// <summary>
/// Titles, Headings, and Captions must NOT end with a period.
/// Ellipsis (...) and other punctuation (?!) are allowed.
/// </summary>
public class NoDotsInTitlesRule : IValidationRule
{
    public string Name => "NoDotsInTitlesRule";

    // Style patterns that this rule applies to (case-insensitive)
    private static readonly string[] TargetStylePatterns =
    [
        "heading", "nagwek",           // Headings (EN/PL)
        "title", "tytu",               // Title (EN/PL)
        "subtitle", "podtytu",         // Subtitle (EN/PL)
        "caption", "podpis"            // Caption (EN/PL)
    ];

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
            return errors;

        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;

            // Only check paragraphs with target styles
            if (!HasTargetStyle(paragraph))
                continue;

            var text = GetParagraphText(paragraph);

            // Skip empty paragraphs
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var trimmedText = text.TrimEnd();

            // Check if ends with a single period (not ellipsis)
            if (EndsWithSinglePeriod(trimmedText))
            {
                var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "Unknown";
                var preview = Truncate(trimmedText, 60);

                var errorMessage = $"Title/Heading should not end with a period. Style: {styleId}. Text: \"{preview}\"";

                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = errorMessage,
                    IsError = true,
                    Location = new DocumentLocation
                    {
                        Paragraph = paragraphIndex,
                        Text = preview
                    }
                });

                documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static bool HasTargetStyle(Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return false;

        var styleLower = styleId.ToLowerInvariant();
        return TargetStylePatterns.Any(pattern => styleLower.Contains(pattern));
    }

    private static bool EndsWithSinglePeriod(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Must end with a period
        if (!text.EndsWith('.'))
            return false;

        // Check it's not an ellipsis (... or more dots)
        if (text.Length >= 2 && text[^2] == '.')
            return false;

        // It's a single trailing period - this is an error
        return true;
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
    }
}
