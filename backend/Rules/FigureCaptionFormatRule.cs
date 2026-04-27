using backend.Models;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Structure;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates the visible text format of existing figure captions.
/// </summary>
public class FigureCaptionFormatRule : IValidationRule
{
    public string Name => "FigureCaptionFormatRule";

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var errors = new List<ValidationResult>();

        foreach (var caption in FigureCaptionDetector.GetDetectedFigureCaptions(doc, config))
        {
            var text = caption.Text.Trim();
            var preview = TextExtractionService.Truncate(text, 50);

            if (!CaptionDetectionService.HasValidFigureCaptionFormat(text))
            {
                var message = "Figure caption has invalid format - use a label such as \"Rys.\" or \"Rysunek\", a number, and a description.";
                errors.Add(MakeResult(config, message, caption.ParagraphIndex, preview));
                commentService?.AddCommentToParagraph(doc, caption.Paragraph, message);
                continue;
            }

            if (CaptionDetectionService.EndsWithSinglePeriod(text))
            {
                var message = "Figure caption should not end with a period.";
                errors.Add(MakeResult(config, message, caption.ParagraphIndex, preview));
                commentService?.AddCommentToParagraph(doc, caption.Paragraph, message);
            }
        }

        return errors;
    }

    private ValidationResult MakeResult(
        UniversityConfig config,
        string message,
        int paragraph,
        string text)
    {
        return ValidationResultFactory.ForParagraph(
            Name,
            config,
            message,
            paragraph,
            text,
            ParagraphIndexKind.Descendant);
    }
}
