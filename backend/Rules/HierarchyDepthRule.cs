using backend.Models;
using backend.RuleOptions;
using backend.Services.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Structure;

namespace Rules;

/// <summary>
/// Hierarchy: The division of the work cannot be deeper than 3 levels.
/// Any usage of Heading 4, Heading 5, etc. is forbidden.
/// </summary>
public class HierarchyDepthRule : IValidationRule
{
    public const string RuleId = nameof(HierarchyDepthRule);

    private readonly IRuleConfigurationService _ruleConfigurationService;
    private readonly HierarchyDepthRuleOptions _options;

    public HierarchyDepthRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<HierarchyDepthRuleOptions>? options = null)
    {
        var hierarchyOptions = options ?? Options.Create(new HierarchyDepthRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                hierarchyDepthOptions: hierarchyOptions);
        _options = hierarchyOptions.Value;
    }

    public string Name => RuleId;

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService = null)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            var level = HeadingDetectionService.GetHeadingLevel(doc, paragraph);
            if (level is null || level <= _options.MaxAllowedLevel)
                continue;

            var text = TextExtractionService.GetParagraphText(doc, paragraph, config);
            var preview = TextExtractionService.Truncate(text, 60);

            var errorMessage = $"Structure too deep. Detected Level {level}, but maximum allowed is {_options.MaxAllowedLevel}.";

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

        return errors;
    }
}
