using System.Text.RegularExpressions;
using backend.Models;
using backend.RuleOptions;
using backend.ModernServices.Extraction;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Rule #13: Only single spaces allowed between words.
/// </summary>
public sealed partial class SingleSpaceRule : ValidationRule<SingleSpaceRuleOptions>
{
    public const string RuleId = nameof(SingleSpaceRule);

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Single Space",
        Description: "Finds places where words are separated by multiple spaces.",
        Category: RuleCategories.Formatting,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    [GeneratedRegex(@"  +", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        SingleSpaceRuleOptions options)
    {
        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph.Text))
                continue;

            foreach (Match match in MultipleSpacesRegex().Matches(paragraph.Text))
            {
                var snippet = GetContextSnippet(paragraph.Text, match.Index, match.Length);
                var spaceCount = match.Length;
                var message =
                    $"Multiple spaces found ({spaceCount} spaces). Only single spaces allowed between words. Context: \"{snippet}\"";

                yield return new RuleProblem(
                    message,
                    new DocumentLocation
                    {
                        Paragraph = paragraph.BodyIndex,
                        CharacterOffset = match.Index,
                        Length = match.Length,
                        Text = snippet
                    },
                    ParagraphIndexKind.BodyElement,
                    new ParagraphAnnotationTarget(paragraph.Paragraph));
            }
        }
    }

    private static string GetContextSnippet(
        string text,
        int matchIndex,
        int matchLength,
        int contextChars = 15)
    {
        var start = Math.Max(0, matchIndex - contextChars);
        var end = Math.Min(text.Length, matchIndex + matchLength + contextChars);

        var snippet = text[start..end];
        var prefix = start > 0 ? "..." : string.Empty;
        var suffix = end < text.Length ? "..." : string.Empty;
        var beforeMatch = snippet[..(matchIndex - start)];
        var afterMatch = snippet[(matchIndex - start + matchLength)..];

        return $"{prefix}{beforeMatch}[{matchLength} spaces]{afterMatch}{suffix}";
    }
}
