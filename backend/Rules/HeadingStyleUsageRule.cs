using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Heading Styles: Chapter titles must use Heading 1, subchapters Heading 2, etc.
/// Detects paragraphs that appear manually formatted as headings
/// (bold + font size above body text) without using a proper Heading style.
/// </summary>
public class HeadingStyleUsageRule : IValidationRule
{
    public string Name => "HeadingStyleUsageRule";

    private const int FontSizeThresholdAboveBodyPt = 2;
    private const int MaxHeadingTextLength = 200;

    private static readonly string[] ExcludedStylePatterns =
    [
        "toc", "tableofcontents",
        "header", "footer",
        "caption", "podpis",
        "title", "tytu",
        "subtitle", "podtytu",
        "listparagraph",
        "footnote", "endnote"
    ];

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null) return errors;

        var bodyFontSizePt = config.Formatting.Font.FontSize;
        var thresholdPt = bodyFontSizePt + FontSizeThresholdAboveBodyPt;

        int paragraphIndex = 0;
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;

            if (HeadingStyleHelper.IsHeading(doc, paragraph))
                continue;

            if (IsExcludedStyle(paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value))
                continue;

            var text = GetParagraphText(paragraph).Trim();

            if (string.IsNullOrWhiteSpace(text) || text.Length > MaxHeadingTextLength)
                continue;

            if (!LooksLikeManualHeading(doc, paragraph, thresholdPt))
                continue;

            var preview = text.Length > 60 ? text[..60] + "..." : text;
            var message =
                "Paragraph appears manually formatted as a heading — " +
                "apply a proper Heading style (Heading 1, Heading 2, etc.) " +
                "instead of manual bold/font-size formatting.";

            errors.Add(new ValidationResult
            {
                RuleName = Name,
                Message = message,
                IsError = true,
                Location = new DocumentLocation
                {
                    Paragraph = paragraphIndex,
                    Text = preview
                }
            });

            commentService?.AddCommentToParagraph(doc, paragraph, message);
        }

        return errors;
    }

    private static bool LooksLikeManualHeading(
        WordprocessingDocument doc,
        Paragraph paragraph,
        double fontSizeThresholdPt)
    {
        var runs = paragraph.Elements<Run>()
            .Where(r => !string.IsNullOrWhiteSpace(GetRunText(r)))
            .ToList();

        if (runs.Count == 0) return false;

        bool allBold = true;
        bool hasLargeFont = false;

        foreach (var run in runs)
        {
            if (!IsRunBold(doc, paragraph, run))
                allBold = false;

            var fontSizePt = ResolveEffectiveFontSizePt(doc, paragraph, run);
            if (fontSizePt is not null && fontSizePt >= fontSizeThresholdPt)
                hasLargeFont = true;
        }

        return allBold && hasLargeFont;
    }

    private static bool IsRunBold(WordprocessingDocument doc, Paragraph paragraph, Run run)
    {
        var bold = run.RunProperties?.Bold;
        if (bold is not null)
            return bold.Val is null || bold.Val.Value;

        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (!string.IsNullOrEmpty(styleId))
        {
            var styleBold = GetStyleRunBold(doc, styleId);
            if (styleBold is not null) return styleBold.Value;
        }

        return false;
    }

    private static bool? GetStyleRunBold(WordprocessingDocument doc, string styleId)
    {
        var style = FindStyle(doc, styleId);
        var bold = style?.StyleRunProperties?.Bold;
        if (bold is not null)
            return bold.Val is null || bold.Val.Value;
        return null;
    }

    private static double? ResolveEffectiveFontSizePt(
        WordprocessingDocument doc,
        Paragraph paragraph,
        Run run)
    {
        var runSize = run.RunProperties?.FontSize?.Val?.Value;
        if (TryParseHalfPoints(runSize, out var runPt))
            return runPt;

        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (!string.IsNullOrEmpty(styleId))
        {
            var stylePt = GetStyleFontSizePt(doc, styleId);
            if (stylePt is not null) return stylePt;
        }

        return GetDefaultFontSizePt(doc);
    }

    private static double? GetStyleFontSizePt(WordprocessingDocument doc, string styleId)
    {
        var style = FindStyle(doc, styleId);
        var sizeVal = style?.StyleRunProperties?.FontSize?.Val?.Value;
        return TryParseHalfPoints(sizeVal, out var pt) ? pt : null;
    }

    private static double? GetDefaultFontSizePt(WordprocessingDocument doc)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var defaultStyle = styles?
            .Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph
                              && s.Default?.Value == true);

        var sizeVal = defaultStyle?.StyleRunProperties?.FontSize?.Val?.Value;
        return TryParseHalfPoints(sizeVal, out var pt) ? pt : null;
    }

    private static Style? FindStyle(WordprocessingDocument doc, string styleId)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        return styles?.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);
    }

    private static bool TryParseHalfPoints(string? value, out double points)
    {
        points = 0;
        if (string.IsNullOrEmpty(value)) return false;
        if (!double.TryParse(value, out var halfPts)) return false;
        points = halfPts / 2.0;
        return true;
    }

    private static bool IsExcludedStyle(string? styleId)
    {
        if (string.IsNullOrEmpty(styleId)) return false;
        var lower = styleId.ToLowerInvariant();
        return ExcludedStylePatterns.Any(lower.Contains);
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static string GetRunText(Run run)
    {
        return string.Concat(run.Elements<Text>().Select(t => t.Text));
    }
}
