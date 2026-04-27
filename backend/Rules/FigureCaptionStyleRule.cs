using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Structure;

namespace backend.Rules;

/// <summary>
/// Validates the Word style and paragraph formatting of existing figure captions.
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

        foreach (var captionCandidate in FigureCaptionDetector.GetDetectedFigureCaptions(doc, config))
        {
            var caption = captionCandidate.Paragraph;
            var captionIdx = captionCandidate.ParagraphIndex;
            var preview = TextExtractionService.Truncate(captionCandidate.Text, 50);
            if (!CaptionDetectionService.UsesDedicatedCaptionStyle(doc, caption))
            {
                var label = CaptionDetectionService.GetCaptionStyleLabel(doc, caption);
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
            ParagraphIndexKind.Descendant);
    }
}
