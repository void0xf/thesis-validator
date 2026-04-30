using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Figures;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates that an existing figure caption is placed below its figure.
/// </summary>
public sealed class FigureCaptionPositionRule : ValidationRule<FigureCaptionPositionRuleOptions>
{
    public const string RuleId = nameof(FigureCaptionPositionRule);

    private readonly FigureCaptionAnalyzer _figureCaptionAnalyzer;

    public FigureCaptionPositionRule(FigureCaptionAnalyzer? figureCaptionAnalyzer = null)
    {
        _figureCaptionAnalyzer = figureCaptionAnalyzer ?? new FigureCaptionAnalyzer();
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

            var preview = TextExtractor.Truncate(association.Caption.Text, 50);
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
