using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Content;
using backend.Models;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Detects figure-like objects that do not have a nearby figure caption.
/// </summary>
public sealed class MissingFigureCaptionRule : ValidationRule<MissingFigureCaptionRuleOptions>
{
    public const string RuleId = nameof(MissingFigureCaptionRule);

    private readonly FigureCaptionAnalyzer _figureCaptionAnalyzer;

    public MissingFigureCaptionRule(FigureCaptionAnalyzer? figureCaptionAnalyzer = null)
    {
        _figureCaptionAnalyzer = figureCaptionAnalyzer ?? new FigureCaptionAnalyzer();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Missing Figure Captions",
        Description: "Finds figures without a nearby structured figure caption.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        MissingFigureCaptionRuleOptions options)
    {
        foreach (var association in _figureCaptionAnalyzer.AssociateFiguresWithCaptions(
                     context,
                     requireStructuredCaption: true))
        {
            if (association.HasCaption)
                continue;

            var message = "Figure has no caption - add a figure caption below the figure.";
            yield return new RuleProblem(
                message,
                new DocumentLocation
                {
                    Paragraph = association.Figure.ParagraphIndex,
                    Text = "[Figure]"
                },
                ParagraphIndexKind.BodyElement,
                new ParagraphAnnotationTarget(association.Figure.Paragraph));
        }
    }
}
