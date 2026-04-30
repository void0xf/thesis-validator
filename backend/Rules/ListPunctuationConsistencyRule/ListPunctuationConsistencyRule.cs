using backend.Models;
using backend.ModernServices;
using backend.RuleOptions;
using backend.ModernServices.Extraction;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates list punctuation consistency.
/// </summary>
public sealed class ListPunctuationConsistencyRule : ValidationRule<ListPunctuationConsistencyRuleOptions>
{
    public const string RuleId = nameof(ListPunctuationConsistencyRule);

    private readonly ModernListAnalyzer _listAnalyzer;

    public ListPunctuationConsistencyRule(ModernListAnalyzer? listAnalyzer = null)
    {
        _listAnalyzer = listAnalyzer ?? new ModernListAnalyzer();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "List Punctuation Consistency",
        Description: "Finds list items with inconsistent trailing punctuation.",
        Category: RuleCategories.Layout,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        ListPunctuationConsistencyRuleOptions options)
    {
        foreach (var list in _listAnalyzer.ExtractLists(context))
        {
            foreach (var problem in ValidatePunctuationConsistency(list))
            {
                yield return problem;
            }
        }
    }

    private static IEnumerable<RuleProblem> ValidatePunctuationConsistency(ModernListGroup list)
    {
        if (list.Items.Count < 2)
            yield break;

        var itemsByLevel = list.Items.GroupBy(item => item.Level);
        foreach (var levelGroup in itemsByLevel)
        {
            var items = levelGroup.ToList();
            if (items.Count < 2)
                continue;

            var firstItem = items.First();
            var lastItem = items.Last();
            var middleItems = items.Count > 2 ? items.Skip(1).Take(items.Count - 2).ToList() : [];
            var expectedPunctuation = GetTrailingPunctuation(firstItem.Text);

            foreach (var item in middleItems)
            {
                var ending = GetTrailingPunctuation(item.Text);
                if (ending == expectedPunctuation)
                    continue;

                var expectedDesc = expectedPunctuation.HasValue
                    ? $"'{expectedPunctuation}'"
                    : "no punctuation";
                var actualDesc = ending.HasValue
                    ? $"'{ending}'"
                    : "no punctuation";
                var preview = GetListItemPreview(item.Text);
                var message =
                    $"List item ends with {actualDesc} but first item uses {expectedDesc}. Text: \"{preview}\"";

                yield return CreateProblem(item, preview, message);
            }

            var lastEnding = GetTrailingPunctuation(lastItem.Text);
            if (lastEnding == '.')
                continue;

            var lastPreview = GetListItemPreview(lastItem.Text);
            var lastMessage = lastEnding.HasValue
                ? $"Last list item should end with period (.), found '{lastEnding}'. Text: \"{lastPreview}\""
                : $"Last list item should end with period (.). Text: \"{lastPreview}\"";

            yield return CreateProblem(lastItem, lastPreview, lastMessage);
        }
    }

    private static RuleProblem CreateProblem(
        ModernListItem item,
        string preview,
        string message)
    {
        return new RuleProblem(
            message,
            new DocumentLocation
            {
                Paragraph = item.ParagraphIndex,
                Text = preview
            },
            ParagraphIndexKind.BodyElement,
            new ParagraphAnnotationTarget(item.Paragraph));
    }

    private static char? GetTrailingPunctuation(string text)
    {
        var trimmed = text.TrimEnd();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        var lastChar = trimmed[^1];
        return char.IsPunctuation(lastChar) ? lastChar : null;
    }

    private static string GetListItemPreview(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? "[empty]"
            : TextExtractionService.Truncate(text, 40);
    }
}
