using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Content;
using backend.Infrastructure.LanguageTool;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates document text for grammar and spelling errors using LanguageTool.
/// </summary>
public sealed class GrammarRule : ValidationRule<GrammarRuleOptions>
{
    public const string RuleId = "Grammar";

    private const string DefaultLanguage = "pl-PL";

    private readonly LanguageToolClient _languageToolService;

    public GrammarRule(LanguageToolClient languageToolService)
    {
        _languageToolService = languageToolService;
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Grammar & Spelling",
        Description: "Finds grammar and spelling issues using LanguageTool.",
        Category: RuleCategories.Language,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        GrammarRuleOptions options)
    {
        return ValidateAsync(context).GetAwaiter().GetResult();
    }

    private async Task<IReadOnlyList<RuleProblem>> ValidateAsync(RuleContext context)
    {
        var problems = new List<RuleProblem>();

        if (!await _languageToolService.IsAvailableAsync())
        {
            problems.Add(new RuleProblem(
                "Grammar check skipped: LanguageTool service is not available",
                new DocumentLocation { Paragraph = 0 },
                ParagraphIndexKind.BodyElement));
            return problems;
        }

        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph.Text))
                continue;

            problems.AddRange(await CheckParagraphGrammarAsync(paragraph));
        }

        return problems;
    }

    private async Task<IEnumerable<RuleProblem>> CheckParagraphGrammarAsync(
        ParagraphNode paragraph)
    {
        var problems = new List<RuleProblem>();

        try
        {
            var response = await _languageToolService.CheckTextAsync(paragraph.Text, DefaultLanguage);

            foreach (var match in response.Matches)
            {
                problems.Add(CreateProblem(match, paragraph));
            }
        }
        catch (Exception ex)
        {
            problems.Add(new RuleProblem(
                $"Grammar check failed for paragraph {paragraph.BodyIndex}: {ex.Message}",
                new DocumentLocation
                {
                    Paragraph = paragraph.BodyIndex
                },
                ParagraphIndexKind.BodyElement,
                new ParagraphAnnotationTarget(paragraph.Paragraph)));
        }

        return problems;
    }

    private static RuleProblem CreateProblem(
        LanguageToolMatch match,
        ParagraphNode paragraph)
    {
        var errorText = paragraph.Text.Substring(
            match.Offset,
            Math.Min(match.Length, paragraph.Text.Length - match.Offset));

        var suggestions = match.Replacements
            .Take(3)
            .Select(replacement => replacement.Value)
            .ToList();

        var suggestionText = suggestions.Count > 0
            ? $" Suggestions: {string.Join(", ", suggestions)}"
            : string.Empty;

        var issueType = GetIssueType(match);
        var message = $"{issueType}: {match.Message}{suggestionText}";

        return new RuleProblem(
            message,
            new DocumentLocation
            {
                Paragraph = paragraph.BodyIndex,
                Run = 1,
                CharacterOffset = match.Offset,
                Length = match.Length,
                Text = TextExtractor.Truncate(errorText, 50)
            },
            ParagraphIndexKind.BodyElement,
            new ParagraphAnnotationTarget(paragraph.Paragraph));
    }

    private static GrammarIssueType GetIssueType(LanguageToolMatch match)
    {
        var issueType = match.Rule?.IssueType?.ToLowerInvariant() ?? string.Empty;
        var categoryId = match.Rule?.Category?.Id?.ToLowerInvariant() ?? string.Empty;

        if (issueType == "misspelling" || categoryId == "typos")
            return GrammarIssueType.Spelling;

        if (categoryId.Contains("grammar") || issueType == "grammar")
            return GrammarIssueType.Grammar;

        if (categoryId.Contains("style") || issueType == "style")
            return GrammarIssueType.Style;

        if (categoryId.Contains("punctuation"))
            return GrammarIssueType.Punctuation;

        if (categoryId.Contains("typography"))
            return GrammarIssueType.Typography;

        return GrammarIssueType.Other;
    }
}

public enum GrammarIssueType
{
    Spelling,
    Grammar,
    Punctuation,
    Style,
    Typography,
    Other
}
