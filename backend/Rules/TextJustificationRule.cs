using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Skipping;

namespace Rules;

/// <summary>
/// Rule #15: Standard text must use full justification (both margins).
/// Exceptions: Lists, Headings, Titles, Subtitles, Captions, and TOC entries.
/// </summary>
public class TextJustificationRule : IValidationRule
{
    private readonly ICodeBlockDetector _codeBlockDetector;

    public string Name => "TextJustificationRule";

    public TextJustificationRule(ICodeBlockDetector? codeBlockDetector = null)
    {
        _codeBlockDetector = codeBlockDetector ?? CodeBlockDetector.CreateDefault();
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
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

                errors.Add(ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    errorMessage,
                    paragraphIndex,
                    preview));

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
