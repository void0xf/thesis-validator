using backend.Models;
using backend.RuleOptions;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace Rules;

/// <summary>
/// Validates list punctuation consistency:
/// Items except the last should end with the same punctuation, and the last item should end with a period.
/// </summary>
public class ListPunctuationConsistencyRule : IValidationRule
{
    public const string RuleId = nameof(ListPunctuationConsistencyRule);

    private readonly IRuleConfigurationService _ruleConfigurationService;

    public string Name => RuleId;

    public ListPunctuationConsistencyRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<ListPunctuationConsistencyRuleOptions>? options = null)
    {
        var punctuationOptions = options ?? Options.Create(new ListPunctuationConsistencyRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                listPunctuationConsistencyOptions: punctuationOptions);
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();
        var lists = ListRuleItemExtractor.ExtractLists(doc, config);

        foreach (var list in lists)
        {
            errors.AddRange(ValidatePunctuationConsistency(doc, list, config, documentCommentService));
        }

        return errors;
    }

    private IEnumerable<ValidationResult> ValidatePunctuationConsistency(
        WordprocessingDocument doc,
        ListGroup list,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();

        if (list.Items.Count < 2)
            return errors;

        var itemsByLevel = list.Items.GroupBy(i => i.Level);

        foreach (var levelGroup in itemsByLevel)
        {
            var items = levelGroup.ToList();
            if (items.Count < 2)
                continue;

            var firstItem = items.First();
            var lastItem = items.Last();
            var middleItems = items.Count > 2 ? items.Skip(1).Take(items.Count - 2).ToList() : [];

            var expectedPunctuation = GetTrailingPunctuation(firstItem.Paragraph, config);

            foreach (var item in middleItems)
            {
                var ending = GetTrailingPunctuation(item.Paragraph, config);
                var text = TextExtractionService.GetParagraphText(doc, item.Paragraph, config);
                var preview = GetListItemPreview(text);

                if (ending != expectedPunctuation)
                {
                    var expectedDesc = expectedPunctuation.HasValue
                        ? $"'{expectedPunctuation}'"
                        : "no punctuation";
                    var actualDesc = ending.HasValue
                        ? $"'{ending}'"
                        : "no punctuation";

                    var errorMessage = $"List item ends with {actualDesc} but first item uses {expectedDesc}. Text: \"{preview}\"";

                    var result = ValidationResultFactory.ForParagraph(
                        Name,
                        config,
                        errorMessage,
                        item.ParagraphIndex,
                        preview,
                        ParagraphIndexKind.BodyElement);
                    result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
                    errors.Add(result);

                    documentCommentService?.AddCommentToParagraph(doc, item.Paragraph, errorMessage);
                }
            }

            var lastEnding = GetTrailingPunctuation(lastItem.Paragraph, config);
            if (lastEnding != '.')
            {
                var lastText = TextExtractionService.GetParagraphText(doc, lastItem.Paragraph, config);
                var lastPreview = GetListItemPreview(lastText);

                var errorMessage = lastEnding.HasValue
                    ? $"Last list item should end with period (.), found '{lastEnding}'. Text: \"{lastPreview}\""
                    : $"Last list item should end with period (.). Text: \"{lastPreview}\"";

                var result = ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    errorMessage,
                    lastItem.ParagraphIndex,
                    lastPreview,
                    ParagraphIndexKind.BodyElement);
                result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
                errors.Add(result);

                documentCommentService?.AddCommentToParagraph(doc, lastItem.Paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static char? GetTrailingPunctuation(Paragraph paragraph, UniversityConfig config)
    {
        var text = TextExtractionService.GetParagraphText(paragraph, config).TrimEnd();
        if (string.IsNullOrEmpty(text))
            return null;

        var lastChar = text[^1];
        return char.IsPunctuation(lastChar) ? lastChar : null;
    }

    private static string GetListItemPreview(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? "[empty]"
            : TextExtractionService.Truncate(text, 40);
    }
}
