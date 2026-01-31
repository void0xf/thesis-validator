using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates document text for grammar and spelling errors using LanguageTool.
/// </summary>
public class GrammarRule : IValidationRule
{
    private readonly LanguageToolService _languageToolService;

    public GrammarRule(LanguageToolService languageToolService)
    {
        _languageToolService = languageToolService;
    }

    public string Name => "Grammar";

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config)
    {
        return Validate(doc, config, null);
    }

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? commentService)
    {
        if (!config.CheckGrammar)
        {
            return Enumerable.Empty<ValidationResult>();
        }

        return ValidateAsync(doc, config, commentService).GetAwaiter().GetResult();
    }

    private async Task<IEnumerable<ValidationResult>> ValidateAsync(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
            return errors;

        if (!await _languageToolService.IsAvailableAsync())
        {
            errors.Add(new ValidationResult
            {
                RuleName = Name,
                IsError = false,
                Message = "Grammar check skipped: LanguageTool service is not available",
                Location = new DocumentLocation { Paragraph = 0 }
            });
            return errors;
        }

        var language = config.Language;
        int paragraphIndex = 0;

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;

            var paragraphText = GetParagraphText(paragraph);
            if (string.IsNullOrWhiteSpace(paragraphText))
                continue;

            var grammarErrors = await CheckParagraphGrammarAsync(
                doc,
                paragraph,
                paragraphText,
                paragraphIndex,
                language,
                commentService);

            errors.AddRange(grammarErrors);
        }

        return errors;
    }

    private async Task<List<ValidationResult>> CheckParagraphGrammarAsync(
        WordprocessingDocument doc,
        Paragraph paragraph,
        string text,
        int paragraphIndex,
        string language,
        DocumentCommentService? commentService)
    {
        var errors = new List<ValidationResult>();

        try
        {
            var response = await _languageToolService.CheckTextAsync(text, language);

            foreach (var match in response.Matches)
            {
                var result = CreateValidationResult(match, paragraph, paragraphIndex, text);
                errors.Add(result);

                // Add comment to document
                if (commentService != null)
                {
                    commentService.AddCommentAtOffset(doc, paragraph, match.Offset, match.Length, result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationResult
            {
                RuleName = Name,
                IsError = false,
                Message = $"Grammar check failed for paragraph {paragraphIndex}: {ex.Message}",
                Location = new DocumentLocation
                {
                    Paragraph = paragraphIndex
                }
            });
        }

        return errors;
    }

    private ValidationResult CreateValidationResult(
        LanguageToolMatch match,
        Paragraph paragraph,
        int paragraphIndex,
        string fullText)
    {
        var errorText = fullText.Substring(
            match.Offset,
            Math.Min(match.Length, fullText.Length - match.Offset));

        var suggestions = match.Replacements
            .Take(3)
            .Select(r => r.Value)
            .ToList();

        var suggestionText = suggestions.Count > 0
            ? $" Suggestions: {string.Join(", ", suggestions)}"
            : string.Empty;

        var issueType = GetIssueType(match);

        return new ValidationResult
        {
            RuleName = Name,
            IsError = issueType == GrammarIssueType.Spelling || issueType == GrammarIssueType.Grammar,
            Message = $"{issueType}: {match.Message}{suggestionText}",
            Location = new DocumentLocation
            {
                Paragraph = paragraphIndex,
                Run = 1,
                CharacterOffset = match.Offset,
                Length = match.Length,
                Text = Truncate(errorText, 50)
            }
        };
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

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
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
