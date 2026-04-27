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
using backend.Services.Structure;

namespace backend.Rules;

public class ParagraphIndentRule : IValidationRule
{
    private readonly ICodeBlockDetector _codeBlockDetector;
    private readonly IRuleConfigurationService _ruleConfigurationService;
    private readonly ParagraphIndentRuleOptions _options;

    public const string RuleId = nameof(LayoutConfig.RequiredIndentCm);

    public string Name => RuleId;

    public ParagraphIndentRule(
        ICodeBlockDetector? codeBlockDetector = null,
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<ParagraphIndentRuleOptions>? options = null)
    {
        var paragraphIndentOptions = options ?? Options.Create(new ParagraphIndentRuleOptions());

        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                paragraphIndentOptions: paragraphIndentOptions);
        _options = paragraphIndentOptions.Value;
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();
        var allowedIndentsTwips = _options.AllowedIndentTwips ?? [];

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph))
                continue;

            if (SkipDecisionService.HasExcludedStructuralStyle(doc, paragraph))
                continue;

            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            if (!TextExtractionService.HasMeaningfulParagraphContent(paragraph, config))
                continue;

            if (IsCenteredOrRightAligned(doc, paragraph))
                continue;

            var firstLineIndent = FormattingResolutionService.ResolveFirstLineIndent(doc, paragraph);
            var startsWithTab = StartsWithTabCharacter(paragraph);

            if (SkipDecisionService.IsListItem(paragraph))
                continue;

            if (startsWithTab)
            {
                var message = $"Paragraph uses TAB character for indent instead of proper first-line indent formatting. Please use paragraph formatting ({FormatAllowedIndents()} first-line indent) instead of TAB.";

                var result = ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    message,
                    paragraphIndex,
                    TextExtractionService.GetPreview(paragraph, config, 50));
                result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
                errors.Add(result);

                documentCommentService?.AddCommentToParagraph(doc, paragraph, message);
                continue;
            }

            if (!IsValidIndent(firstLineIndent, allowedIndentsTwips))
            {
                var actualIndentCm = firstLineIndent / UnitConversion.TwipsPerCm;
                var message = $"Paragraph has incorrect first line indent: {actualIndentCm:F2} cm. Expected {FormatAllowedIndents()}.";

                var result = ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    message,
                    paragraphIndex,
                    TextExtractionService.GetPreview(paragraph, config, 50));
                result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
                errors.Add(result);

                documentCommentService?.AddCommentToParagraph(doc, paragraph, message);
            }
        }

        return errors;
    }

    private bool IsValidIndent(int actualTwips, int[] allowedTwips)
    {
        return allowedTwips.Any(allowed => Math.Abs(actualTwips - allowed) <= _options.ToleranceTwips);
    }

    private string FormatAllowedIndents()
    {
        var allowedIndentsTwips = _options.AllowedIndentTwips ?? [];
        return allowedIndentsTwips.Length == 0
            ? "a configured indent"
            : string.Join(" or ", allowedIndentsTwips.Select(indent => $"{indent / UnitConversion.TwipsPerCm:F2} cm"));
    }

    private static bool StartsWithTabCharacter(Paragraph paragraph)
    {
        foreach (var child in paragraph.Descendants())
        {
            if (child is TabChar)
                return true;

            if (child is Text text)
            {
                var textDecision = StartsWithTextTab(text.Text);
                if (textDecision.HasValue)
                    return textDecision.Value;
            }
        }

        return false;
    }

    private static bool? StartsWithTextTab(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        foreach (var ch in text)
        {
            if (ch == '\t')
                return true;

            if (!char.IsWhiteSpace(ch) && !char.IsControl(ch))
                return false;
        }

        return null;
    }

    private static bool IsCenteredOrRightAligned(WordprocessingDocument doc, Paragraph paragraph)
    {
        var justification = FormattingResolutionService.ResolveJustification(doc, paragraph);
        return justification == JustificationValues.Center || justification == JustificationValues.Right;
    }
}
