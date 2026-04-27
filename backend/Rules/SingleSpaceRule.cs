using System.Text.RegularExpressions;
using backend.Models;
using backend.RuleOptions;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Rules;

namespace Rules;

/// <summary>
/// Rule #13: Only single spaces allowed between words.
/// (Polish: Odstęp między wyrazami jedna spacja)
/// </summary>
public partial class SingleSpaceRule : IValidationRule
{
    public const string RuleId = nameof(SingleSpaceRule);

    private readonly ICodeBlockDetector _codeBlockDetector;
    private readonly IRuleConfigurationService _ruleConfigurationService;

    public string Name => RuleId;

    public SingleSpaceRule(
        ICodeBlockDetector? codeBlockDetector = null,
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<SingleSpaceRuleOptions>? options = null)
    {
        var singleSpaceOptions = options ?? Options.Create(new SingleSpaceRuleOptions());

        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                singleSpaceOptions: singleSpaceOptions);
    }

    // Regex to find 2 or more consecutive spaces
    [GeneratedRegex(@"  +", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        var errors = new List<ValidationResult>();
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (CodeBlockRuleSkipper.ShouldSkip(doc, paragraph, _codeBlockDetector))
                continue;

            var text = TextExtractionService.GetParagraphText(doc, paragraph, config);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var matches = MultipleSpacesRegex().Matches(text);

            foreach (Match match in matches)
            {
                var snippet = GetContextSnippet(text, match.Index, match.Length);
                var spaceCount = match.Length;

                var errorMessage = $"Multiple spaces found ({spaceCount} spaces). Only single spaces allowed between words. Context: \"{snippet}\"";

                var result = ValidationResultFactory.Create(
                    Name,
                    config,
                    errorMessage,
                    new DocumentLocation
                    {
                        Paragraph = paragraphIndex,
                        CharacterOffset = match.Index,
                        Length = match.Length,
                        Text = snippet
                    });
                result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
                errors.Add(result);

                documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static string GetContextSnippet(string text, int matchIndex, int matchLength, int contextChars = 15)
    {
        var start = Math.Max(0, matchIndex - contextChars);
        var end = Math.Min(text.Length, matchIndex + matchLength + contextChars);

        var snippet = text[start..end];

        var prefix = start > 0 ? "..." : "";
        var suffix = end < text.Length ? "..." : "";

        var beforeMatch = snippet[..(matchIndex - start)];
        var theMatch = snippet[(matchIndex - start)..(matchIndex - start + matchLength)];
        var afterMatch = snippet[(matchIndex - start + matchLength)..];

        return $"{prefix}{beforeMatch}[{matchLength} spaces]{afterMatch}{suffix}";
    }
}
