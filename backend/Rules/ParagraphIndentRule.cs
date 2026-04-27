using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Skipping;
using backend.Services.Structure;

namespace backend.Rules;

public class ParagraphIndentRule : IValidationRule
{
    private readonly ICodeBlockDetector _codeBlockDetector;

    public string Name => nameof(LayoutConfig.RequiredIndentCm);

    private const int ToleranceTwips = 60;

    public ParagraphIndentRule(ICodeBlockDetector? codeBlockDetector = null)
    {
        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        var errors = new List<ValidationResult>();
        var allowedIndentsTwips = new[] { 567, 709 };

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph))
                continue;

            if (SkipDecisionService.HasExcludedStructuralStyle(doc, paragraph))
                continue;

            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            if (!TextExtractionService.HasMeaningfulParagraphContent(paragraph, config))
                continue;

            if (IsCenteredOrRightAligned(doc, paragraph))
                continue;

            var firstLineIndent = FormattingResolutionService.ResolveFirstLineIndent(doc, paragraph);
            var startsWithTab = StartsWithTabCharacter(paragraph);

            if (firstLineIndent == 0 && SkipDecisionService.IsListItem(paragraph))
                continue;

            if (startsWithTab && firstLineIndent == 0)
            {
                var message = "Paragraph uses TAB character for indent instead of proper first-line indent formatting. Please use paragraph formatting (1.00 cm or 1.25 cm first-line indent) instead of TAB.";

                errors.Add(ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    message,
                    paragraphIndex,
                    TextExtractionService.GetPreview(paragraph, config, 50)));

                documentCommentService?.AddCommentToParagraph(doc, paragraph, message);
                continue;
            }

            if (!IsValidIndent(firstLineIndent, allowedIndentsTwips))
            {
                var actualIndentCm = firstLineIndent / UnitConversion.TwipsPerCm;
                var message = $"Paragraph has incorrect first line indent: {actualIndentCm:F2} cm. Expected 1.00 cm or 1.25 cm.";

                errors.Add(ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    message,
                    paragraphIndex,
                    TextExtractionService.GetPreview(paragraph, config, 50)));

                documentCommentService?.AddCommentToParagraph(doc, paragraph, message);
            }
        }

        return errors;
    }

    private static bool IsValidIndent(int actualTwips, int[] allowedTwips)
    {
        return allowedTwips.Any(allowed => Math.Abs(actualTwips - allowed) <= ToleranceTwips);
    }

    private static bool StartsWithTabCharacter(Paragraph paragraph)
    {
        var firstRun = paragraph.Elements<Run>().FirstOrDefault();
        if (firstRun == null)
            return false;

        var firstChild = firstRun.Elements().FirstOrDefault();
        if (firstChild is TabChar)
            return true;

        foreach (var child in firstRun.Elements())
        {
            if (child is TabChar)
                return true;
            if (child is Text)
                break;
        }

        return false;
    }

    private static bool IsCenteredOrRightAligned(WordprocessingDocument doc, Paragraph paragraph)
    {
        var justification = FormattingResolutionService.ResolveJustification(doc, paragraph);
        return justification == JustificationValues.Center || justification == JustificationValues.Right;
    }
}
