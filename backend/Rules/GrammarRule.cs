using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Language;
using backend.Services.Results;

namespace backend.Rules;

/// <summary>
/// Validates document text for grammar and spelling errors using LanguageTool.
/// </summary>
public class GrammarRule : IValidationRule
{
    private readonly LanguageToolService _languageToolService;
    private readonly ICodeBlockDetector _codeBlockDetector;

    public GrammarRule(
        LanguageToolService languageToolService,
        ICodeBlockDetector? codeBlockDetector = null)
    {
        _languageToolService = languageToolService;
        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
    }

    public string Name => "Grammar";

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config)
    {
        return Validate(doc, config, null);
    }

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? commentService)
    {
        return ValidateAsync(doc, config, commentService).GetAwaiter().GetResult();
    }

    private async Task<IEnumerable<ValidationResult>> ValidateAsync(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService)
    {
        var errors = new List<ValidationResult>();
        if (!await _languageToolService.IsAvailableAsync())
        {
            errors.Add(ValidationResultFactory.Create(
                Name,
                config,
                "Grammar check skipped: LanguageTool service is not available",
                new DocumentLocation { Paragraph = 0 },
                ParagraphIndexKind.BodyElement,
                ValidationSeverity.Warning));
            return errors;
        }

        var language = config.Language;

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            var paragraphText = TextExtractionService.GetParagraphText(doc, paragraph, config);
            if (string.IsNullOrWhiteSpace(paragraphText))
                continue;

            var grammarErrors = await CheckParagraphGrammarAsync(
                doc,
                paragraph,
                paragraphText,
                paragraphIndex,
                config,
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
        UniversityConfig config,
        string language,
        DocumentCommentService? commentService)
    {
        var errors = new List<ValidationResult>();

        try
        {
            var response = await _languageToolService.CheckTextAsync(text, language);

            foreach (var match in response.Matches)
            {
                var result = CreateValidationResult(match, config, paragraphIndex, text);
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
            errors.Add(ValidationResultFactory.Create(
                Name,
                config,
                $"Grammar check failed for paragraph {paragraphIndex}: {ex.Message}",
                new DocumentLocation
                {
                    Paragraph = paragraphIndex
                },
                ParagraphIndexKind.BodyElement,
                ValidationSeverity.Warning));
        }

        return errors;
    }

    private ValidationResult CreateValidationResult(
        LanguageToolMatch match,
        UniversityConfig config,
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

        var severity = issueType == GrammarIssueType.Spelling || issueType == GrammarIssueType.Grammar
            ? ValidationSeverity.Error
            : ValidationSeverity.Warning;

        return ValidationResultFactory.ForRun(
            Name,
            config,
            $"{issueType}: {match.Message}{suggestionText}",
            paragraphIndex,
            1,
            match.Offset,
            match.Length,
            TextExtractionService.Truncate(errorText, 50),
            ParagraphIndexKind.BodyElement,
            severity);
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
