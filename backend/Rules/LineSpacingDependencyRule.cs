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
using backend.Services.Skipping;
using backend.Services.Structure;

namespace Rules;

/// <summary>
/// Rule #11: If line spacing is 1.5, then paragraph spacing before and after must be 0.
/// </summary>
public class LineSpacingDependencyRule : IValidationRule
{
    private const int LineSpacing15 = 360;

    public string Name => "LineSpacingDependencyRule";

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph))
                continue;

            if (SkipDecisionService.HasExcludedStructuralStyle(paragraph))
                continue;

            var (lineSpacing, lineRule) = FormattingResolutionService.ResolveLineSpacing(doc, paragraph);
            if (!IsLineSpacing15(lineSpacing, lineRule))
                continue;

            var (spacingBefore, spacingAfter) = FormattingResolutionService.ResolveParagraphSpacing(doc, paragraph);

            if (spacingBefore != 0 || spacingAfter != 0)
            {
                var beforePt = UnitConversion.TwipsToPoints(spacingBefore);
                var afterPt = UnitConversion.TwipsToPoints(spacingAfter);
                var preview = TextExtractionService.GetPreview(paragraph, config, 50);
                if (string.IsNullOrWhiteSpace(preview))
                    continue;

                var errorMessage = $"Paragraph with 1.5 line spacing must have 0pt spacing before and after. " +
                                   $"Found: Before={beforePt:F1}pt, After={afterPt:F1}pt.";

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

    private static bool IsLineSpacing15(int? lineSpacing, LineSpacingRuleValues? lineRule)
    {
        if (!lineSpacing.HasValue)
            return false;

        return (lineRule == null || lineRule == LineSpacingRuleValues.Auto)
            && lineSpacing.Value == LineSpacing15;
    }
}
