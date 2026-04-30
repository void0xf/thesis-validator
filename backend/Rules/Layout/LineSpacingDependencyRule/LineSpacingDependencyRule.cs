using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Figures;
using backend.Models;
using backend.DocumentProcessing.Content;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Rule #11: Body text must use the configured line spacing.
/// </summary>
public sealed class LineSpacingDependencyRule : ValidationRule<LineSpacingDependencyRuleOptions>
{
    public const string RuleId = nameof(LineSpacingDependencyRule);

    private readonly FormattingResolver _formattingResolver;
    private readonly ParagraphClassifier _paragraphClassifier;

    public LineSpacingDependencyRule(
        FormattingResolver? formattingResolver = null,
        ParagraphClassifier? paragraphClassifier = null)
    {
        _formattingResolver = formattingResolver ?? new FormattingResolver();
        _paragraphClassifier = paragraphClassifier ?? new ParagraphClassifier();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Line Spacing",
        Description: "Finds body paragraphs that do not use the required line spacing.",
        Category: RuleCategories.Layout,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        LineSpacingDependencyRuleOptions options)
    {
        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (paragraph.IsHeading)
                continue;

            if (_paragraphClassifier.HasExcludedStructuralStyle(context.RawDocument, paragraph.Paragraph))
                continue;

            var (lineSpacing, lineRule) = _formattingResolver.ResolveLineSpacing(
                context.RawDocument,
                paragraph.Paragraph);
            if (IsTargetLineSpacing(lineSpacing, lineRule, options.TargetLineSpacingTwips))
                continue;

            var preview = TextExtractor.Truncate(paragraph.Text, 50);
            if (string.IsNullOrWhiteSpace(preview))
                continue;

            var message = $"Paragraph line spacing must be {FormatAutoLineSpacing(options.TargetLineSpacingTwips)}. " +
                          $"Found: {FormatLineSpacing(lineSpacing, lineRule)}.";

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

    private static bool IsTargetLineSpacing(
        int? lineSpacing,
        LineSpacingRuleValues? lineRule,
        int targetLineSpacingTwips)
    {
        if (!lineSpacing.HasValue)
            return false;

        return (lineRule == null || lineRule == LineSpacingRuleValues.Auto)
            && lineSpacing.Value == targetLineSpacingTwips;
    }

    private static string FormatLineSpacing(int? lineSpacing, LineSpacingRuleValues? lineRule)
    {
        if (!lineSpacing.HasValue)
            return "not set";

        if (lineRule is null || lineRule == LineSpacingRuleValues.Auto)
            return FormatAutoLineSpacing(lineSpacing.Value);

        return $"{lineSpacing.Value} with {lineRule.Value} line rule";
    }

    private static string FormatAutoLineSpacing(int lineSpacing)
    {
        return $"{lineSpacing / 240.0:F1}";
    }
}
