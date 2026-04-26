using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Skipping;
using backend.Services.Structure;

namespace backend.Rules;

/// <summary>
/// Detects paragraphs that look manually formatted as headings without using a Heading style.
/// </summary>
public class HeadingStyleUsageRule : IValidationRule
{
    public string Name => "HeadingStyleUsageRule";

    private const int FontSizeThresholdAboveBodyPt = 2;
    private const int MaxHeadingTextLength = 200;

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var errors = new List<ValidationResult>();
        var bodyFontSizePt = config.Formatting.Font.FontSize;
        var thresholdPt = bodyFontSizePt + FontSizeThresholdAboveBodyPt;

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph))
                continue;

            if (SkipDecisionService.HasExcludedStructuralStyle(paragraph))
                continue;

            var text = TextExtractionService.GetParagraphText(doc, paragraph, config).Trim();

            if (string.IsNullOrWhiteSpace(text) || text.Length > MaxHeadingTextLength)
                continue;

            if (!LooksLikeManualHeading(doc, paragraph, config, thresholdPt))
                continue;

            var preview = TextExtractionService.Truncate(text, 60);
            var message =
                "Paragraph appears manually formatted as a heading - " +
                "apply a proper Heading style (Heading 1, Heading 2, etc.) " +
                "instead of manual bold/font-size formatting.";

            errors.Add(ValidationResultFactory.ForParagraph(
                Name,
                config,
                message,
                paragraphIndex,
                preview,
                ParagraphIndexKind.BodyElement));

            commentService?.AddCommentToParagraph(doc, paragraph, message);
        }

        return errors;
    }

    private static bool LooksLikeManualHeading(
        WordprocessingDocument doc,
        Paragraph paragraph,
        UniversityConfig config,
        double fontSizeThresholdPt)
    {
        var runs = paragraph.Elements<Run>()
            .Where(run => !string.IsNullOrWhiteSpace(TextExtractionService.GetRunText(run, config)))
            .ToList();

        if (runs.Count == 0)
            return false;

        bool allBold = true;
        bool hasLargeFont = false;

        foreach (var run in runs)
        {
            if (!FormattingResolutionService.IsRunBold(doc, paragraph, run))
                allBold = false;

            var fontSizePt = FormattingResolutionService.ResolveFontSizePt(doc, paragraph, run);
            if (fontSizePt is not null && fontSizePt >= fontSizeThresholdPt)
                hasLargeFont = true;
        }

        return allBold && hasLargeFont;
    }
}
