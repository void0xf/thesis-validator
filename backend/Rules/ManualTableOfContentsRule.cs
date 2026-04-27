using backend.Models;
using backend.RuleOptions;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Rules;
using backend.Services.Structure;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace Rules;

public class ManualTableOfContentsRule : IValidationRule
{
    public const string RuleId = "Manual table of contents";

    private const string ManualTableOfContentsMessage =
        "A table of contents section was detected, but no automatic Word TOC field was found. The table of contents was probably created manually and may become inconsistent with the document structure.";

    private readonly IRuleConfigurationService _ruleConfigurationService;

    public string Name => RuleId;

    public ManualTableOfContentsRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<ManualTableOfContentsRuleOptions>? options = null)
    {
        var manualTableOfContentsOptions = options ?? Options.Create(new ManualTableOfContentsRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                manualTableOfContentsOptions: manualTableOfContentsOptions);
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var detection = TableOfContentsDetectionService.Detect(doc);
        if (detection.Kind != TableOfContentsKind.Manual)
            return [];

        var result = ValidationResultFactory.ForParagraph(
            Name,
            config,
            ManualTableOfContentsMessage,
            detection.ParagraphIndex,
            TextExtractionService.Truncate(detection.Text ?? string.Empty, 80),
            severity: ValidationSeverity.Warning);
        result.Severity = _ruleConfigurationService.ResolveSeverity(
            Name,
            config,
            ValidationSeverity.Warning);

        if (detection.Paragraph is not null && documentCommentService is not null)
            documentCommentService.AddCommentToParagraph(doc, detection.Paragraph, ManualTableOfContentsMessage);

        return [result];
    }
}
