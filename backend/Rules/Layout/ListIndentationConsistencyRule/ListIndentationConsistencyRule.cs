using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Figures;
using backend.Models;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Formatting;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates that list items at the same level use identical indentation.
/// </summary>
public sealed class ListIndentationConsistencyRule : ValidationRule<ListIndentationConsistencyRuleOptions>
{
    public const string RuleId = nameof(ListIndentationConsistencyRule);

    private readonly ListAnalyzer _listAnalyzer;

    public ListIndentationConsistencyRule(ListAnalyzer? listAnalyzer = null)
    {
        _listAnalyzer = listAnalyzer ?? new ListAnalyzer();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "List Indentation Consistency",
        Description: "Finds list items at the same level that use different left indentation.",
        Category: RuleCategories.Layout,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        ListIndentationConsistencyRuleOptions options)
    {
        foreach (var list in _listAnalyzer.ExtractLists(context))
        {
            foreach (var problem in ValidateIndentationConsistency(list))
            {
                yield return problem;
            }
        }
    }

    private static IEnumerable<RuleProblem> ValidateIndentationConsistency(ListGroup list)
    {
        var itemsByLevel = list.Items.GroupBy(item => item.Level);

        foreach (var levelGroup in itemsByLevel)
        {
            var items = levelGroup.ToList();
            if (items.Count < 2)
                continue;

            var indentCounts = items
                .GroupBy(item => item.IndentLeft)
                .OrderByDescending(group => group.Count())
                .ToList();

            if (indentCounts.Count <= 1)
                continue;

            var expectedIndent = indentCounts.First().Key;
            foreach (var item in items.Where(item => item.IndentLeft != expectedIndent))
            {
                var preview = TextExtractor.Truncate(item.Text, 40);
                var expectedCm = UnitConversion.TwipsToCentimeters(expectedIndent);
                var actualCm = UnitConversion.TwipsToCentimeters(item.IndentLeft);
                var message =
                    $"List item has inconsistent indentation ({actualCm:F2} cm). " +
                    $"Expected {expectedCm:F2} cm at level {item.Level}. Text: \"{preview}\"";

                yield return new RuleProblem(
                    message,
                    new DocumentLocation
                    {
                        Paragraph = item.ParagraphIndex,
                        Text = preview
                    },
                    ParagraphIndexKind.BodyElement,
                    new ParagraphAnnotationTarget(item.Paragraph));
            }
        }
    }
}
