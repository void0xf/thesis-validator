using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Figures;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class TocRule : ValidationRule<TocRuleOptions>
{
    public const string RuleId = nameof(TocRule);

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Automatic Table of Contents",
        Description: "Finds documents that do not contain an automatic Word table of contents.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        TocRuleOptions options)
    {
        var detection = TableOfContentsDetector.Detect(context.RawDocument);
        if (detection.Kind == TableOfContentsKind.Automatic)
            return [];

        var body = context.RawDocument.MainDocumentPart?.Document.Body;
        var firstRun = body?.Descendants<Run>().FirstOrDefault();

        return
        [
            new RuleProblem(
                "Document is missing an automatic Word Table of Contents.",
                new DocumentLocation(),
                ParagraphIndexKind.BodyElement,
                firstRun is null ? null : new RunAnnotationTarget(firstRun))
        ];
    }

    public static TableOfContentsDetection DetectTableOfContents(RuleContext context)
    {
        return TableOfContentsDetector.Detect(context.RawDocument);
    }

    public static bool IsTableOfContentsHeadingText(string text)
    {
        return TableOfContentsDetector.IsTableOfContentsHeadingText(text);
    }
}
