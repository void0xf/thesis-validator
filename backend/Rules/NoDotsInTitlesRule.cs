using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;

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
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (!HasTargetStyle(paragraph))
                continue;

            var text = TextExtractionService.GetParagraphText(doc, paragraph, config);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var trimmedText = text.TrimEnd();

            // Check if ends with a single period (not ellipsis)
            if (EndsWithSinglePeriod(trimmedText))
            {
                var styleId = StyleResolutionService.GetParagraphStyleId(paragraph) ?? "Unknown";
                var preview = TextExtractionService.Truncate(trimmedText, 60);

                var errorMessage = $"Title/Heading should not end with a period. Style: {styleId}. Text: \"{preview}\"";

                errors.Add(ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    errorMessage,
                    paragraphIndex,
                    preview));

                documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static bool HasTargetStyle(Paragraph paragraph)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
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

}
