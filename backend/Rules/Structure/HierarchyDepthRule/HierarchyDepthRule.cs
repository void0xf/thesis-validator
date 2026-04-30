using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Figures;
using backend.Models;
using backend.DocumentProcessing.Content;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// The division of the work cannot be deeper than the configured heading level.
/// </summary>
public sealed class HierarchyDepthRule : ValidationRule<HierarchyDepthRuleOptions>
{
    public const string RuleId = nameof(HierarchyDepthRule);

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Heading Hierarchy",
        Description: "Finds heading levels deeper than the configured maximum.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        HierarchyDepthRuleOptions options)
    {
        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            var level = paragraph.HeadingLevel;
            if (level is null || level <= options.MaxAllowedLevel)
                continue;

            var preview = TextExtractor.Truncate(paragraph.Text, 60);
            var message =
                $"Structure too deep. Detected Level {level}, but maximum allowed is {options.MaxAllowedLevel}.";

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
