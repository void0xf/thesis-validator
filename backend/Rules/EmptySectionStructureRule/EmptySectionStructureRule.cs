using backend.Models;
using backend.RuleOptions;
using backend.Services.Extraction;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// A subchapter heading (e.g. Heading 2) cannot immediately follow its parent
/// chapter heading (e.g. Heading 1) without any intervening body text.
/// Every section must contain at least a brief introductory paragraph
/// before the first sub-section begins.
/// </summary>
public sealed class EmptySectionStructureRule : ValidationRule<NoRuleOptions>
{
    public const string RuleId = "EmptySectionStructureRule";

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Empty Sections",
        Description: "Finds headings/sections with no content.",
        Category: RuleCategories.Language,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        NoRuleOptions options)
    {
        foreach (var section in FlattenSections(context.Content.Sections))
        {
            var firstChild = section.Children.FirstOrDefault();
            if (firstChild is null || section.HasIntroductoryContent)
                continue;

            var parentTitle = TextExtractionService.Truncate(section.Title, 60);
            var childTitle = TextExtractionService.Truncate(firstChild.Title, 50);

            var message =
                $"Heading {section.Level} \"{parentTitle}\" " +
                $"is immediately followed by Heading {firstChild.Level} \"{childTitle}\" " +
                "with no introductory text. Add at least one paragraph of body text " +
                "before the first sub-section.";

            yield return new RuleProblem(
                message,
                new DocumentLocation
                {
                    Paragraph = section.Heading.BodyIndex,
                    Text = parentTitle
                },
                ParagraphIndexKind.BodyElement,
                new ParagraphAnnotationTarget(section.Heading.Paragraph));
        }
    }

    private static IEnumerable<SectionNode> FlattenSections(IEnumerable<SectionNode> sections)
    {
        foreach (var section in sections)
        {
            yield return section;

            foreach (var child in FlattenSections(section.Children))
            {
                yield return child;
            }
        }
    }
}
