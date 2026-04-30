using backend.Models;
using backend.ModernServices;
using backend.RuleOptions;
using backend.ModernServices.Extraction;
using backend.ModernServices.Structure;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Detects paragraphs that look manually formatted as headings without using a Heading style.
/// </summary>
public sealed class HeadingStyleUsageRule : ValidationRule<HeadingStyleUsageRuleOptions>
{
    public const string RuleId = nameof(HeadingStyleUsageRule);

    private const double DefaultBodyFontSizePt = 12.0;

    private readonly ModernFormattingResolver _formattingResolver;
    private readonly ModernParagraphClassifier _paragraphClassifier;

    public HeadingStyleUsageRule(
        ModernFormattingResolver? formattingResolver = null,
        ModernParagraphClassifier? paragraphClassifier = null)
    {
        _formattingResolver = formattingResolver ?? new ModernFormattingResolver();
        _paragraphClassifier = paragraphClassifier ?? new ModernParagraphClassifier();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Heading Style Usage",
        Description: "Finds paragraphs that look like manually formatted headings.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Warning);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        HeadingStyleUsageRuleOptions options)
    {
        var thresholdPt = DefaultBodyFontSizePt + options.FontSizeThresholdAboveBodyPt;

        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (paragraph.IsHeading || HeadingDetectionService.IsHeading(context.RawDocument, paragraph.Paragraph))
                continue;

            if (_paragraphClassifier.HasExcludedStructuralStyle(context.RawDocument, paragraph.Paragraph))
                continue;

            var text = paragraph.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length > options.MaxHeadingTextLength)
                continue;

            if (!LooksLikeManualHeading(context, paragraph.Paragraph, thresholdPt))
                continue;

            var preview = TextExtractionService.Truncate(text, 60);
            var message =
                "Paragraph appears manually formatted as a heading - " +
                "apply a proper Heading style (Heading 1, Heading 2, etc.) " +
                "instead of manual bold/font-size formatting.";

            yield return new RuleProblem(
                message,
                new DocumentLocation
                {
                    Paragraph = paragraph.BodyIndex,
                    Text = preview
                },
                ParagraphIndexKind.BodyElement,
                new ParagraphAnnotationTarget(paragraph.Paragraph));
        }
    }

    private bool LooksLikeManualHeading(
        RuleContext context,
        Paragraph paragraph,
        double fontSizeThresholdPt)
    {
        var runs = paragraph.Elements<Run>()
            .Where(run => !string.IsNullOrWhiteSpace(GetRunText(run)))
            .ToList();

        if (runs.Count == 0)
            return false;

        var allBold = true;
        var hasLargeFont = false;

        foreach (var run in runs)
        {
            if (!_formattingResolver.IsRunBold(context.RawDocument, paragraph, run))
                allBold = false;

            var fontSizePt = _formattingResolver.ResolveFontSizePt(context.RawDocument, paragraph, run);
            if (fontSizePt is not null && fontSizePt >= fontSizeThresholdPt)
                hasLargeFont = true;
        }

        return allBold && hasLargeFont;
    }

    private static string GetRunText(Run run)
    {
        return string.Concat(run.Elements<Text>().Select(text => text.Text));
    }
}
