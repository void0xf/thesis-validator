using backend.Models;
using backend.Services.Comments;
using backend.Services.Results;
using backend.Services.Structure;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Detects figure-like objects that do not have a nearby figure caption.
/// </summary>
public class MissingFigureCaptionRule : IValidationRule
{
    public string Name => "MissingFigureCaptionRule";

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var errors = new List<ValidationResult>();

        foreach (var association in FigureCaptionDetector.AssociateFiguresWithCaptions(
                     doc,
                     config,
                     requireStructuredCaption: true))
        {
            if (association.HasCaption)
                continue;

            var message = "Figure has no caption - add a figure caption below the figure.";
            errors.Add(ValidationResultFactory.ForParagraph(
                Name,
                config,
                message,
                association.Figure.ParagraphIndex,
                "[Figure]",
                ParagraphIndexKind.Descendant));

            commentService?.AddCommentToParagraph(doc, association.Figure.Paragraph, message);
        }

        return errors;
    }
}
