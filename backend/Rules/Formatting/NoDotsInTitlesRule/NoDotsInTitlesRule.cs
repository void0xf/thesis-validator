using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Figures;
using backend.Models;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Formatting;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Titles, headings, and captions must not end with a period.
/// </summary>
public sealed class NoDotsInTitlesRule : ValidationRule<NoDotsInTitlesRuleOptions>
{
    public const string RuleId = nameof(NoDotsInTitlesRule);

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Title Punctuation",
        Description: "Finds titles, headings, and captions that end with a single period.",
        Category: RuleCategories.Formatting,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        NoDotsInTitlesRuleOptions options)
    {
        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (!HasTargetStyle(paragraph.Paragraph, options))
                continue;

            if (string.IsNullOrWhiteSpace(paragraph.Text))
                continue;

            var trimmedText = paragraph.Text.TrimEnd();
            if (!EndsWithSinglePeriod(trimmedText))
                continue;

            var styleId = StyleResolver.GetParagraphStyleId(paragraph.Paragraph) ?? "Unknown";
            var preview = TextExtractor.Truncate(trimmedText, 60);
            var message =
                $"Title/Heading should not end with a period. Style: {styleId}. Text: \"{preview}\"";

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

    private static bool HasTargetStyle(Paragraph paragraph, NoDotsInTitlesRuleOptions options)
    {
        var styleId = StyleResolver.GetParagraphStyleId(paragraph);
        if (string.IsNullOrEmpty(styleId))
            return false;

        var styleLower = styleId.ToLowerInvariant();
        return (options.TargetStylePatterns ?? [])
            .Any(pattern => !string.IsNullOrWhiteSpace(pattern)
                && styleLower.Contains(pattern.Trim().ToLowerInvariant()));
    }

    private static bool EndsWithSinglePeriod(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return text.EndsWith('.') && (text.Length < 2 || text[^2] != '.');
    }
}
