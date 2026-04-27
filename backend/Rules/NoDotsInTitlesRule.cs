using backend.Models;
using backend.RuleOptions;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Rules;

namespace Rules;

/// <summary>
/// Titles, Headings, and Captions must NOT end with a period.
/// Ellipsis (...) and other punctuation (?!) are allowed.
/// </summary>
public class NoDotsInTitlesRule : IValidationRule
{
    public const string RuleId = nameof(NoDotsInTitlesRule);

    private readonly IRuleConfigurationService _ruleConfigurationService;
    private readonly NoDotsInTitlesRuleOptions _options;

    public string Name => RuleId;

    public NoDotsInTitlesRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<NoDotsInTitlesRuleOptions>? options = null)
    {
        var noDotsOptions = options ?? Options.Create(new NoDotsInTitlesRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                noDotsInTitlesOptions: noDotsOptions);
        _options = noDotsOptions.Value;
    }

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (!HasTargetStyle(paragraph))
                continue;

            var text = TextExtractionService.GetParagraphText(doc, paragraph, config);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var trimmedText = text.TrimEnd();

            // Check if ends with a single period (not ellipsis)
            if (EndsWithSinglePeriod(trimmedText))
            {
                var styleId = StyleResolutionService.GetParagraphStyleId(paragraph) ?? "Unknown";
                var preview = TextExtractionService.Truncate(trimmedText, 60);

                var errorMessage = $"Title/Heading should not end with a period. Style: {styleId}. Text: \"{preview}\"";

                var result = ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    errorMessage,
                    paragraphIndex,
                    preview);
                result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
                errors.Add(result);

                documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }

    private bool HasTargetStyle(Paragraph paragraph)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
        if (string.IsNullOrEmpty(styleId))
            return false;

        var styleLower = styleId.ToLowerInvariant();
        return (_options.TargetStylePatterns ?? [])
            .Any(pattern => !string.IsNullOrWhiteSpace(pattern)
                && styleLower.Contains(pattern.Trim().ToLowerInvariant()));
    }

    private static bool EndsWithSinglePeriod(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Must end with a period
        if (!text.EndsWith('.'))
            return false;

        // Check it's not an ellipsis (... or more dots)
        if (text.Length >= 2 && text[^2] == '.')
            return false;

        // It's a single trailing period - this is an error
        return true;
    }

}
