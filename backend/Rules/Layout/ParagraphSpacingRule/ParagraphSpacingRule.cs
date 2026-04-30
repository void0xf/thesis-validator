using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Formatting;
using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class ParagraphSpacingRule : ValidationRule<ParagraphSpacingRuleOptions>
{
    public const string RuleId = nameof(ParagraphSpacingRule);

    private readonly FormattingResolver _formattingResolver;
    private readonly ParagraphClassifier _paragraphClassifier;

    public ParagraphSpacingRule(
        FormattingResolver? formattingResolver = null,
        ParagraphClassifier? paragraphClassifier = null)
    {
        _formattingResolver = formattingResolver ?? new FormattingResolver();
        _paragraphClassifier = paragraphClassifier ?? new ParagraphClassifier();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Paragraph Spacing",
        Description: "Finds body paragraphs whose spacing after value is not allowed.",
        Category: RuleCategories.Layout,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        ParagraphSpacingRuleOptions options)
    {
        var allowedSpacingTwips = options.AllowedSpacingPoints
            .Select(UnitConversion.PointsToTwips)
            .ToHashSet();

        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (paragraph.IsHeading)
                continue;

            if (_paragraphClassifier.HasExcludedStructuralStyle(context.RawDocument, paragraph.Paragraph))
                continue;

            var spacingAfter = _formattingResolver.ResolveSpacingAfter(
                context.RawDocument,
                paragraph.Paragraph);
            var preview = TextExtractor.Truncate(paragraph.Text, 50);

            if (allowedSpacingTwips.Contains(spacingAfter) || string.IsNullOrWhiteSpace(preview))
                continue;

            var expectedPts = string.Join(" or ", options.AllowedSpacingPoints.Select(pt => $"{pt}pt"));
            var actualPt = UnitConversion.TwipsToPoints(spacingAfter);
            var message =
                $"Paragraph has incorrect spacing. After value: {actualPt:F1}pt ({spacingAfter} twips). Expected {expectedPts}.";

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
}
