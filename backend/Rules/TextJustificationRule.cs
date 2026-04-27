using backend.Models;
using backend.RuleOptions;
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
using backend.Services.Rules;
using backend.Services.Skipping;

namespace Rules;

/// <summary>
/// Rule #15: Standard text must use full justification (both margins).
/// Exceptions: Lists, Headings, Titles, Subtitles, Captions, and TOC entries.
/// </summary>
public class TextJustificationRule : IValidationRule
{
    public const string RuleId = nameof(TextJustificationRule);

    private readonly ICodeBlockDetector _codeBlockDetector;
    private readonly IRuleConfigurationService _ruleConfigurationService;

    public string Name => RuleId;

    public TextJustificationRule(
        ICodeBlockDetector? codeBlockDetector = null,
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<TextJustificationRuleOptions>? options = null)
    {
        var textJustificationOptions = options ?? Options.Create(new TextJustificationRuleOptions());

        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                textJustificationOptions: textJustificationOptions);
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
            var text = TextExtractionService.GetParagraphText(doc, paragraph, config);
            if (!TextExtractionService.HasMeaningfulContent(text))
                continue;

            if (SkipDecisionService.IsListItem(paragraph))
                continue;

            if (SkipDecisionService.HasExcludedStructuralStyle(paragraph))
                continue;

            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            var justification = FormattingResolutionService.ResolveJustification(doc, paragraph);

            if (justification != JustificationValues.Both)
            {
                var alignmentName = GetAlignmentName(justification);
                var preview = TextExtractionService.Truncate(text, 50);
                var errorMessage = $"Paragraph is {alignmentName} aligned. Standard text must use full justification (both margins).";

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

    private static string GetAlignmentName(JustificationValues? justification)
    {
        if (justification == JustificationValues.Left)
            return "left";
        if (justification == JustificationValues.Right)
            return "right";
        if (justification == JustificationValues.Center)
            return "center";
        if (justification == JustificationValues.Both)
            return "fully justified";
        if (justification == JustificationValues.Distribute)
            return "distributed";
        return "left";
    }
}
