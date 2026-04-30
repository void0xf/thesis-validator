using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Figures;
using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class ManualTableOfContentsRule : ValidationRule<ManualTableOfContentsRuleOptions>
{
    public const string RuleId = "ManualTableOfContents";

    private const string ManualTableOfContentsMessage =
        "A table of contents section was detected, but no automatic Word TOC field was found. The table of contents was probably created manually and may become inconsistent with the document structure.";

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Manual Table of Contents",
        Description: "Finds manually created table-of-contents sections.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Warning);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        ManualTableOfContentsRuleOptions options)
    {
        var detection = TableOfContentsDetector.Detect(context.RawDocument);
        if (detection.Kind != TableOfContentsKind.Manual)
            return [];

        return
        [
            new RuleProblem(
                ManualTableOfContentsMessage,
                new DocumentLocation
                {
                    Paragraph = detection.ParagraphIndex,
                    Text = TextExtractor.Truncate(detection.Text ?? string.Empty, 80)
                },
                ParagraphIndexKind.Descendant,
                detection.Paragraph is null ? null : new ParagraphAnnotationTarget(detection.Paragraph))
        ];
    }
}
