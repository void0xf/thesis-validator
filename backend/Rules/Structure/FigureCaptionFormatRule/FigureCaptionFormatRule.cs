using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.Models;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Figures;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates the visible text format of existing figure captions.
/// </summary>
public sealed class FigureCaptionFormatRule : ValidationRule<FigureCaptionFormatRuleOptions>
{
    public const string RuleId = nameof(FigureCaptionFormatRule);

    private readonly FigureCaptionAnalyzer _figureCaptionAnalyzer = new();

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
            var preview = TextExtractor.Truncate(text, 50);

            if (!CaptionDetection.HasValidFigureCaptionFormat(text))
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

            if (CaptionDetection.EndsWithSinglePeriod(text))
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
