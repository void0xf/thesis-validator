using backend.Models;
using backend.RuleOptions;
using backend.Services.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Skipping;
using backend.Services.Structure;

namespace Rules;

/// <summary>
/// Rule #11: Body text must use the configured line spacing.
/// </summary>
public class LineSpacingDependencyRule : IValidationRule
{
    public const string RuleId = nameof(LineSpacingDependencyRule);

    private readonly ICodeBlockDetector _codeBlockDetector;
    private readonly IRuleConfigurationService _ruleConfigurationService;
    private readonly LineSpacingDependencyRuleOptions _options;

    public string Name => RuleId;

    public LineSpacingDependencyRule(
        ICodeBlockDetector? codeBlockDetector = null,
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<LineSpacingDependencyRuleOptions>? options = null)
    {
        var lineSpacingOptions = options ?? Options.Create(new LineSpacingDependencyRuleOptions());

        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                lineSpacingDependencyOptions: lineSpacingOptions);
        _options = lineSpacingOptions.Value;
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph))
                continue;

            if (SkipDecisionService.HasExcludedStructuralStyle(paragraph))
                continue;

            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            var (lineSpacing, lineRule) = FormattingResolutionService.ResolveLineSpacing(doc, paragraph);
            if (IsTargetLineSpacing(lineSpacing, lineRule))
                continue;

            var preview = TextExtractionService.GetPreview(paragraph, config, 50);
            if (string.IsNullOrWhiteSpace(preview))
                continue;

            var errorMessage = $"Paragraph line spacing must be {FormatRequiredLineSpacing()}. " +
                               $"Found: {FormatLineSpacing(lineSpacing, lineRule)}.";

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

    private bool IsTargetLineSpacing(int? lineSpacing, LineSpacingRuleValues? lineRule)
    {
        if (!lineSpacing.HasValue)
            return false;

        return (lineRule == null || lineRule == LineSpacingRuleValues.Auto)
            && lineSpacing.Value == _options.TargetLineSpacingTwips;
    }

    private string FormatRequiredLineSpacing()
    {
        return FormatAutoLineSpacing(_options.TargetLineSpacingTwips);
    }

    private static string FormatLineSpacing(int? lineSpacing, LineSpacingRuleValues? lineRule)
    {
        if (!lineSpacing.HasValue)
            return "not set";

        if (lineRule is null || lineRule == LineSpacingRuleValues.Auto)
            return FormatAutoLineSpacing(lineSpacing.Value);

        return $"{lineSpacing.Value} with {lineRule.Value} line rule";
    }

    private static string FormatAutoLineSpacing(int lineSpacing)
    {
        return $"{lineSpacing / 240.0:F1}";
    }
}
