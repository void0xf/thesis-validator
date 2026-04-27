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
/// Warns when a visually valid figure caption appears to be manually numbered.
/// </summary>
public class FigureCaptionAutomaticNumberingRule : IValidationRule
{
    public string Name => "FigureCaptionAutomaticNumberingRule";

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var warnings = new List<ValidationResult>();

        foreach (var caption in FigureCaptionDetector.GetDetectedFigureCaptions(doc, config))
        {
            var text = caption.Text.Trim();
            if (!CaptionDetectionService.HasValidFigureCaptionFormat(text))
                continue;

            if (!caption.UsesDedicatedCaptionStyle)
                continue;

            if (caption.HasFigureSequenceField)
                continue;

            var message =
                "Podpis rysunku wygl\u0105da na wpisany r\u0119cznie. Zalecane jest u\u017cycie funkcji Worda 'Wstaw podpis', aby numeracja rysunk\u00f3w by\u0142a automatyczna.";
            warnings.Add(ValidationResultFactory.ForParagraph(
                Name,
                config,
                message,
                caption.ParagraphIndex,
                TextExtractionService.Truncate(text, 50),
                ParagraphIndexKind.Descendant,
                ValidationSeverity.Warning));

            commentService?.AddCommentToParagraph(doc, caption.Paragraph, message);
        }

        return warnings;
    }
}
