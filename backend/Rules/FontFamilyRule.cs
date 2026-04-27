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

namespace backend.Rules;

public class FontFamilyValidationRule : IValidationRule
{
    public const string RuleId = nameof(FontConfig.FontFamily);

    private readonly ICodeBlockDetector _codeBlockDetector;
    private readonly IRuleConfigurationService _ruleConfigurationService;
    private readonly FontFamilyRuleOptions _options;

    public string Name => RuleId;

    public FontFamilyValidationRule(
        ICodeBlockDetector? codeBlockDetector = null,
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<FontFamilyRuleOptions>? options = null)
    {
        var fontFamilyOptions = options ?? Options.Create(new FontFamilyRuleOptions());

        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                fontFamilyOptions);
        _options = fontFamilyOptions.Value;
    }

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config)
    {
        return Validate(doc, config, null);
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var expectedFont = string.IsNullOrWhiteSpace(_options.RequiredFontFamily)
            ? config.Formatting.Font.FontFamily
            : _options.RequiredFontFamily.Trim();
        var errors = new List<ValidationResult>();

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            ValidateParagraph(doc, paragraph, paragraphIndex, expectedFont, config, errors, commentService);
        }

        return errors;
    }

    private void ValidateParagraph(
        WordprocessingDocument doc,
        Paragraph paragraph,
        int paragraphIndex,
        string expectedFont,
        UniversityConfig config,
        List<ValidationResult> errors,
        DocumentCommentService? commentService)
    {
        int runIndex = 0;
        int characterOffset = 0;

        foreach (var run in paragraph.Elements<Run>())
        {
            runIndex++;
            var text = TextExtractionService.GetRunText(run, config);

            if (!string.IsNullOrWhiteSpace(text))
            {
                var actualFont = FormattingResolutionService.ResolveFontFamily(doc, paragraph, run);

                if (!string.Equals(actualFont, expectedFont, StringComparison.OrdinalIgnoreCase))
                {
                    var message = $"Invalid font '{actualFont ?? "unknown"}' found, expected '{expectedFont}'";

                    commentService?.AddCommentToRun(doc, run, message);

                    var result = ValidationResultFactory.ForRun(
                        Name,
                        config,
                        message,
                        paragraphIndex,
                        runIndex,
                        characterOffset,
                        text.Length,
                        TextExtractionService.Truncate(text, 50),
                        ParagraphIndexKind.BodyElement);
                    result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
                    errors.Add(result);
                }
            }

            characterOffset += text.Length;
        }
    }
}
