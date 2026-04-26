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
using backend.Services.Structure;

namespace backend.Rules;

/// <summary>
/// Validates that every figure is immediately followed by a caption with expected caption formatting.
/// </summary>
public class FigureCaptionStyleRule : IValidationRule
{
    public string Name => "FigureCaptionStyleRule";

    private const double ExpectedFontSizePt = 11.0;
    private const int IndentToleranceTwips = 10;

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var errors = new List<ValidationResult>();
        var paragraphs = DocumentAnalysisScope.BodyParagraphs(doc, config).ToList();

        for (int i = 0; i < paragraphs.Count; i++)
        {
            var (figureParagraph, figureIdx) = paragraphs[i];
            if (!FigureDetectionService.ContainsImage(figureParagraph, config))
                continue;

            if (i + 1 >= paragraphs.Count)
            {
                AddMissingCaption(doc, config, errors, figureParagraph, figureIdx, commentService);
                continue;
            }

            var (caption, captionIdx) = paragraphs[i + 1];
            var captionText = TextExtractionService.GetParagraphText(doc, caption, config).Trim();

            if (string.IsNullOrWhiteSpace(captionText))
            {
                AddMissingCaption(doc, config, errors, figureParagraph, figureIdx, commentService);
                continue;
            }

            var preview = TextExtractionService.Truncate(captionText, 50);
            if (!CaptionDetectionService.UsesDedicatedCaptionStyle(caption))
            {
                var label = CaptionDetectionService.GetCaptionStyleLabel(caption);
                var msg = $"Figure caption uses \"{label}\" style - assign a Caption style (e.g., \"Caption\", \"Legenda\").";
                errors.Add(MakeResult(config, msg, captionIdx, preview));
                commentService?.AddCommentToParagraph(doc, caption, msg);
            }

            CheckFontSize(doc, config, caption, captionIdx, preview, errors);
            CheckAlignment(doc, config, caption, captionIdx, preview, errors);
            CheckIndentation(doc, config, caption, captionIdx, preview, errors);
        }

        return errors;
    }

    private void AddMissingCaption(
        WordprocessingDocument doc,
        UniversityConfig config,
        List<ValidationResult> errors,
        Paragraph imageParagraph,
        int paragraphIndex,
        DocumentCommentService? commentService)
    {
        var msg = "Figure has no caption - add a caption paragraph with text immediately after the image.";
        errors.Add(MakeResult(config, msg, paragraphIndex, "[Image]"));
        commentService?.AddCommentToParagraph(doc, imageParagraph, msg);
    }

    private void CheckFontSize(
        WordprocessingDocument doc,
        UniversityConfig config,
        Paragraph caption,
        int paraIndex,
        string preview,
        List<ValidationResult> errors)
    {
        var textRun = CaptionDetectionService.GetFirstTextRun(caption, config);
        if (textRun is null)
            return;

        var pt = FormattingResolutionService.ResolveFontSizePt(doc, caption, textRun);
        if (pt is null)
            return;

        if (Math.Abs(pt.Value - ExpectedFontSizePt) > 0.01)
        {
            errors.Add(MakeResult(
                config,
                $"Figure caption font size must be 11pt, found {pt:0.##}pt.",
                paraIndex,
                preview));
        }
    }

    private void CheckAlignment(
        WordprocessingDocument doc,
        UniversityConfig config,
        Paragraph caption,
        int paraIndex,
        string preview,
        List<ValidationResult> errors)
    {
        var justification = FormattingResolutionService.ResolveJustification(
            doc,
            caption,
            includeDefaultStyle: false);
        if (justification == JustificationValues.Center)
            return;

        var name = justification == JustificationValues.Right
            ? "right-aligned"
            : justification == JustificationValues.Both
                ? "justified"
                : "left-aligned";

        errors.Add(MakeResult(
            config,
            $"Figure caption must be centered, found {name}.",
            paraIndex,
            preview));
    }

    private void CheckIndentation(
        WordprocessingDocument doc,
        UniversityConfig config,
        Paragraph caption,
        int paraIndex,
        string preview,
        List<ValidationResult> errors)
    {
        var (left, firstLine) = FormattingResolutionService.ResolveIndentation(
            doc,
            caption,
            includeDefaultStyle: false);
        if (Math.Abs(left) <= IndentToleranceTwips && Math.Abs(firstLine) <= IndentToleranceTwips)
            return;

        errors.Add(MakeResult(
            config,
            $"Figure caption must have no indentation (left: {left / UnitConversion.TwipsPerCm:F2}cm, first-line: {firstLine / UnitConversion.TwipsPerCm:F2}cm).",
            paraIndex,
            preview));
    }

    private ValidationResult MakeResult(
        UniversityConfig config,
        string message,
        int paragraph,
        string text)
    {
        return ValidationResultFactory.ForParagraph(
            Name,
            config,
            message,
            paragraph,
            text,
            ParagraphIndexKind.BodyElement);
    }
}
