using backend.Models;
using backend.RuleOptions;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace Rules;

/// <summary>
/// Validates that all list items at the same level use identical indentation.
/// </summary>
public class ListIndentationConsistencyRule : IValidationRule
{
    public const string RuleId = nameof(ListIndentationConsistencyRule);

    private readonly IRuleConfigurationService _ruleConfigurationService;

    public string Name => RuleId;

    public ListIndentationConsistencyRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<ListIndentationConsistencyRuleOptions>? options = null)
    {
        var indentationOptions = options ?? Options.Create(new ListIndentationConsistencyRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                listIndentationConsistencyOptions: indentationOptions);
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
            errors.AddRange(ValidateIndentationConsistency(doc, list, config, documentCommentService));
        }

        return errors;
    }

    private IEnumerable<ValidationResult> ValidateIndentationConsistency(
        WordprocessingDocument doc,
        ListGroup list,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var itemsByLevel = list.Items.GroupBy(i => i.Level);

        foreach (var levelGroup in itemsByLevel)
        {
            var items = levelGroup.ToList();
            if (items.Count < 2)
                continue;

            var indentCounts = items
                .GroupBy(i => i.IndentLeft)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (indentCounts.Count <= 1)
                continue;

            var expectedIndent = indentCounts.First().Key;

            foreach (var item in items.Where(i => i.IndentLeft != expectedIndent))
            {
                var text = TextExtractionService.GetParagraphText(doc, item.Paragraph, config);
                var preview = TextExtractionService.Truncate(text, 40);

                var expectedCm = UnitConversion.TwipsToCentimeters(expectedIndent);
                var actualCm = UnitConversion.TwipsToCentimeters(item.IndentLeft);

                var errorMessage = $"List item has inconsistent indentation ({actualCm:F2} cm). " +
                                   $"Expected {expectedCm:F2} cm at level {item.Level}. Text: \"{preview}\"";

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

        return errors;
    }
}
