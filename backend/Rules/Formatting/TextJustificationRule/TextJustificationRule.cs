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
/// Rule #15: Standard text must use full justification.
/// </summary>
public sealed class TextJustificationRule : ValidationRule<TextJustificationRuleOptions>
{
    public const string RuleId = nameof(TextJustificationRule);

    private readonly FormattingResolver _formattingResolver;
    private readonly ParagraphClassifier _paragraphClassifier;

    public TextJustificationRule(
        FormattingResolver? formattingResolver = null,
        ParagraphClassifier? paragraphClassifier = null)
    {
        _formattingResolver = formattingResolver ?? new FormattingResolver();
        _paragraphClassifier = paragraphClassifier ?? new ParagraphClassifier();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Text Justification",
        Description: "Finds standard body paragraphs that are not fully justified.",
        Category: RuleCategories.Formatting,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        TextJustificationRuleOptions options)
    {
        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (!TextExtractor.HasMeaningfulContent(paragraph.Text))
                continue;

            if (_paragraphClassifier.IsListItem(paragraph.Paragraph))
                continue;

            if (_paragraphClassifier.HasExcludedStructuralStyle(context.RawDocument, paragraph.Paragraph))
                continue;

            var justification = _formattingResolver.ResolveJustification(
                context.RawDocument,
                paragraph.Paragraph);

            if (justification == JustificationValues.Both)
                continue;

            var alignmentName = GetAlignmentName(justification);
            var preview = TextExtractor.Truncate(paragraph.Text, 50);
            var message =
                $"Paragraph is {alignmentName} aligned. Standard text must use full justification (both margins).";

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

    private static string GetAlignmentName(JustificationValues justification)
    {
        if (justification == JustificationValues.Left)
            return "left";
        if (justification == JustificationValues.Right)
            return "right";
        if (justification == JustificationValues.Center)
            return "center";
        if (justification == JustificationValues.Both)
            return "fully justified";
        if (justification == JustificationValues.Distribute)
            return "distributed";
        return "left";
    }
}
