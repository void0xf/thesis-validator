using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Tables;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Detects inserted tables that do not have an adjacent table caption.
/// </summary>
public sealed class MissingTableCaptionRule : ValidationRule<MissingTableCaptionRuleOptions>
{
    public const string RuleId = nameof(MissingTableCaptionRule);

    private readonly DocumentSkipResolver? _skipResolver;

    public MissingTableCaptionRule(DocumentSkipResolver? skipResolver = null)
    {
        _skipResolver = skipResolver;
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Missing Table Captions",
        Description: "Finds tables without an adjacent table caption.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        MissingTableCaptionRuleOptions options)
    {
        var body = context.RawDocument.MainDocumentPart?.Document.Body;
        if (body is null)
            yield break;

        var children = body.ChildElements;
        var firstIncludedChildIndex = _skipResolver?.GetFirstIncludedBodyChildIndex(context.RawDocument) ?? 0;
        var paragraphIndex = 0;

        for (var childIndex = 0; childIndex < children.Count; childIndex++)
        {
            var child = children[childIndex];
            if (child is Paragraph)
                paragraphIndex++;

            if (childIndex < firstIncludedChildIndex)
                continue;

            if (child is not Table table || !TableCaptionDetection.IsRealTable(table))
                continue;

            if (TableCaptionDetection.HasCaptionImmediatelyAbove(children, childIndex)
                || TableCaptionDetection.HasCaptionImmediatelyBelow(children, childIndex))
            {
                continue;
            }

            yield return new RuleProblem(
                "Table has no caption - add a table caption above the table.",
                new DocumentLocation
                {
                    Paragraph = paragraphIndex,
                    Text = "[Table]"
                },
                ParagraphIndexKind.BodyElement,
                new TableAnnotationTarget(table));
        }
    }
}
