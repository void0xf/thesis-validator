using backend.Models;
using backend.ModernServices;
using backend.RuleOptions;
using backend.ModernServices.Extraction;
using backend.ModernServices.Structure;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates the visible text format of existing figure captions.
/// </summary>
public sealed class FigureCaptionFormatRule : ValidationRule<FigureCaptionFormatRuleOptions>
{
    public const string RuleId = nameof(FigureCaptionFormatRule);

    private readonly ModernFigureCaptionAnalyzer _figureCaptionAnalyzer = new();

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Figure Caption Format",
        Description: "Finds figure captions whose visible text has an invalid format.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        FigureCaptionFormatRuleOptions options)
    {
        foreach (var caption in _figureCaptionAnalyzer.GetDetectedFigureCaptions(context))
        {
            var text = caption.Text.Trim();
            var preview = TextExtractionService.Truncate(text, 50);

            if (!CaptionDetectionService.HasValidFigureCaptionFormat(text))
            {
                yield return new RuleProblem(
                    "Figure caption has invalid format - use a label such as \"Rys.\" or \"Rysunek\", a number, and a description.",
                    new DocumentLocation
                    {
                        Paragraph = caption.ParagraphIndex,
                        Text = preview
                    },
                    ParagraphIndexKind.BodyElement,
                    new ParagraphAnnotationTarget(caption.Paragraph));
                continue;
            }

            if (CaptionDetectionService.EndsWithSinglePeriod(text))
            {
                yield return new RuleProblem(
                    "Figure caption should not end with a period.",
                    new DocumentLocation
                    {
                        Paragraph = caption.ParagraphIndex,
                        Text = preview
                    },
                    ParagraphIndexKind.BodyElement,
                    new ParagraphAnnotationTarget(caption.Paragraph));
            }
        }
    }
}
