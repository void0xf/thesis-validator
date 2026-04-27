using backend.Models;
using backend.RuleOptions;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;
using backend.Services.Comments;
using backend.Services.Results;
using backend.Services.Rules;
using backend.Services.Structure;

namespace Rules;

public class TocRule : IValidationRule
{
    public const string RuleId = nameof(FormattingConfig.CheckTableOfContents);

    private readonly IRuleConfigurationService _ruleConfigurationService;

    public string Name => RuleId;

    public TocRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<TocRuleOptions>? options = null)
    {
        var tocOptions = options ?? Options.Create(new TocRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                tocOptions: tocOptions);
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();
        var detection = DetectTableOfContents(doc);

        if (detection.Kind == TableOfContentsKind.Automatic)
            return errors;

        var body = doc.MainDocumentPart?.Document.Body;
        var firstRun = body?.Descendants<Run>().FirstOrDefault();
        var missingTocResult = ValidationResultFactory.Create(
            Name,
            config,
            "Document is missing an automatic Word Table of Contents.");
        missingTocResult.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
        errors.Add(missingTocResult);

        if (firstRun != null && documentCommentService != null)
            documentCommentService.AddCommentToRun(doc, firstRun, "Document is missing an automatic Word Table of Contents");

        return errors;
    }

    public static TableOfContentsDetection DetectTableOfContents(WordprocessingDocument doc)
    {
        return TableOfContentsDetectionService.Detect(doc);
    }

    public static bool IsTableOfContentsHeadingText(string text)
    {
        return TableOfContentsDetectionService.IsTableOfContentsHeadingText(text);
    }
}
