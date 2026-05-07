using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Tables;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates that table captions are placed above tables.
/// </summary>
public sealed class TableCaptionPositionRule : ValidationRule<TableCaptionPositionRuleOptions>
{
    public const string RuleId = nameof(TableCaptionPositionRule);

    private readonly DocumentSkipResolver? _skipResolver;

    public TableCaptionPositionRule(DocumentSkipResolver? skipResolver = null)
    {
        _skipResolver = skipResolver;
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Table Caption Position",
        Description: "Finds table captions that appear immediately below tables.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Warning);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        TableCaptionPositionRuleOptions options)
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

            if (TableCaptionDetection.HasCaptionImmediatelyAbove(children, childIndex))
                continue;

            if (TableCaptionDetection.GetNextBodyParagraph(children, childIndex) is not { } captionBelow)
                continue;

            if (!TableCaptionDetection.LooksLikeTableCaption(captionBelow))
                continue;

            var captionParagraphIndex = paragraphIndex + 1;
            var preview = TextExtractor.GetPreview(captionBelow, skipTextBoxes: true, maxLength: 60);

            yield return new RuleProblem(
                "Table caption appears to be placed below the table. Table captions should be placed above tables.",
                new DocumentLocation
                {
                    Paragraph = captionParagraphIndex,
                    Text = preview
                },
                ParagraphIndexKind.BodyElement,
                new ParagraphAnnotationTarget(captionBelow));
        }
    }
}
