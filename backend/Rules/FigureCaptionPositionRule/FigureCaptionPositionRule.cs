using backend.Models;
using backend.ModernServices;
using backend.RuleOptions;
using backend.ModernServices.Extraction;
using backend.ModernServices.Structure;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates that an existing figure caption is placed below its figure.
/// </summary>
public sealed class FigureCaptionPositionRule : ValidationRule<FigureCaptionPositionRuleOptions>
{
    public const string RuleId = nameof(FigureCaptionPositionRule);

    private readonly ModernFigureCaptionAnalyzer _figureCaptionAnalyzer;

    public FigureCaptionPositionRule(ModernFigureCaptionAnalyzer? figureCaptionAnalyzer = null)
    {
        _figureCaptionAnalyzer = figureCaptionAnalyzer ?? new ModernFigureCaptionAnalyzer();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Figure Caption Position",
        Description: "Finds figure captions that are above or separated from their figure.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        FigureCaptionPositionRuleOptions options)
    {
        foreach (var association in _figureCaptionAnalyzer.AssociateFiguresWithCaptions(
                     context,
                     requireStructuredCaption: true))
        {
            if (association.Caption is null || association.IsCaptionBelowFigure)
                continue;

            var preview = TextExtractionService.Truncate(association.Caption.Text, 50);
            var message = association.Relation == FigureCaptionRelationKind.Above
                ? "Figure caption should be placed below the figure."
                : "Figure caption should be placed directly below the figure without intervening content.";

            yield return new RuleProblem(
                message,
                new DocumentLocation
                {
                    Paragraph = association.Caption.ParagraphIndex,
                    Text = preview
                },
                ParagraphIndexKind.BodyElement,
                new ParagraphAnnotationTarget(association.Caption.Paragraph));
        }
    }
}
