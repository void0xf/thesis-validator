using backend.Models;
using backend.RuleOptions;
using backend.Services.Comments;
using backend.Services.Results;
using backend.Services.Rules;
using backend.Services.Structure;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Detects figure-like objects that do not have a nearby figure caption.
/// </summary>
public class MissingFigureCaptionRule : IValidationRule
{
    public const string RuleId = nameof(MissingFigureCaptionRule);

    private readonly IRuleConfigurationService _ruleConfigurationService;

    public string Name => RuleId;

    public MissingFigureCaptionRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<MissingFigureCaptionRuleOptions>? options = null)
    {
        var missingFigureCaptionOptions = options ?? Options.Create(new MissingFigureCaptionRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                missingFigureCaptionOptions: missingFigureCaptionOptions);
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();

        foreach (var association in FigureCaptionDetector.AssociateFiguresWithCaptions(
                     doc,
                     config,
                     requireStructuredCaption: true))
        {
            if (association.HasCaption)
                continue;

            var message = "Figure has no caption - add a figure caption below the figure.";
            var result = ValidationResultFactory.ForParagraph(
                Name,
                config,
                message,
                association.Figure.ParagraphIndex,
                "[Figure]",
                ParagraphIndexKind.Descendant);
            result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
            errors.Add(result);

            commentService?.AddCommentToParagraph(doc, association.Figure.Paragraph, message);
        }

        return errors;
    }
}
