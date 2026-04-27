using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Skipping;
using backend.Services.Structure;

namespace Rules;

public class ParagraphSpacingRule : IValidationRule
{
    private readonly ICodeBlockDetector _codeBlockDetector;

    public string Name => nameof(LayoutConfig.ParagraphSpacingRule);

    public ParagraphSpacingRule(ICodeBlockDetector? codeBlockDetector = null)
    {
        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var allowedSpacingTwips = config.Formatting.Layout.ParagraphSpacingRule
            .Select(UnitConversion.PointsToTwips)
            .ToHashSet();

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph))
                continue;

            if (SkipDecisionService.HasExcludedStructuralStyle(paragraph))
                continue;

            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            var spacingAfter = FormattingResolutionService.ResolveSpacingAfter(doc, paragraph);
            var preview = TextExtractionService.GetPreview(paragraph, config, 50);

            if (!allowedSpacingTwips.Contains(spacingAfter)
                && !string.IsNullOrWhiteSpace(preview))
            {
                var expectedPts = string.Join(" or ", config.Formatting.Layout.ParagraphSpacingRule.Select(pt => $"{pt}pt"));
                var actualPt = UnitConversion.TwipsToPoints(spacingAfter);
                var errorMessage = $"Paragraph has incorrect spacing. After value: {actualPt:F1}pt ({spacingAfter} twips). Expected {expectedPts}.";

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
}
