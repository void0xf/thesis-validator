using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates that every figure (image/drawing) is immediately followed by a caption
/// paragraph that uses a dedicated Caption style and meets formatting requirements:
///   • Style is not "Normal" (must be e.g. "Caption", "Legenda")
///   • Font size: 11 pt
///   • Alignment: Centered
///   • Indentation: None (left = 0, first-line = 0)
/// </summary>
public class FigureCaptionStyleRule : IValidationRule
{
    public string Name => "FigureCaptionStyleRule";

    private const double ExpectedFontSizePt = 11.0;
    private const double TwipsPerCm = 567.0;
    private const int IndentToleranceTwips = 10;

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null) return errors;

        var paragraphs = body.Elements<Paragraph>().ToList();

        for (int i = 0; i < paragraphs.Count; i++)
        {
            if (!ContainsImage(paragraphs[i]))
                continue;

            var figureIdx = i + 1; // 1-based for reporting

            // ── Rule 1: caption paragraph must exist ──
            if (i + 1 >= paragraphs.Count)
            {
                AddMissingCaption(doc, errors, paragraphs[i], figureIdx, commentService);
                continue;
            }

            var caption = paragraphs[i + 1];
            var captionText = GetParagraphText(caption).Trim();

            if (string.IsNullOrWhiteSpace(captionText))
            {
                AddMissingCaption(doc, errors, paragraphs[i], figureIdx, commentService);
                continue;
            }

            var captionIdx = figureIdx + 1;
            var preview = Truncate(captionText, 50);
            var styleId = caption.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

            // ── Rule 2: style must not be Normal / absent ──
            if (string.IsNullOrEmpty(styleId) || IsNormalStyle(styleId))
            {
                var label = string.IsNullOrEmpty(styleId) ? "Normal" : styleId;
                var msg = $"Figure caption uses \"{label}\" style — assign a Caption style (e.g., \"Caption\", \"Legenda\").";
                errors.Add(MakeResult(msg, captionIdx, preview));
                commentService?.AddCommentToParagraph(doc, caption, msg);
            }

            // ── Rule 3a: font size = 11 pt ──
            CheckFontSize(doc, caption, styleId, captionIdx, preview, errors);

            // ── Rule 3b: centered alignment ──
            CheckAlignment(doc, caption, styleId, captionIdx, preview, errors);

            // ── Rule 3c: no indentation ──
            CheckIndentation(doc, caption, styleId, captionIdx, preview, errors);
        }

        return errors;
    }

    // ------------------------------------------------------------------ //
    //  Image detection
    // ------------------------------------------------------------------ //

    private static bool ContainsImage(Paragraph paragraph)
    {
        // DrawingML images (<w:drawing>)
        if (paragraph.Descendants<Drawing>().Any())
            return true;

        // Legacy VML images (<w:pict>)
        if (paragraph.Descendants<Picture>().Any())
            return true;

        return false;
    }

    // ------------------------------------------------------------------ //
    //  Missing caption helper
    // ------------------------------------------------------------------ //

    private static void AddMissingCaption(
        WordprocessingDocument doc,
        List<ValidationResult> errors,
        Paragraph imageParagraph,
        int paragraphIndex,
        DocumentCommentService? commentService)
    {
        var msg = "Figure has no caption — add a caption paragraph with text immediately after the image.";
        errors.Add(MakeResult(msg, paragraphIndex, "[Image]"));
        commentService?.AddCommentToParagraph(doc, imageParagraph, msg);
    }

    // ------------------------------------------------------------------ //
    //  Rule 2 helper
    // ------------------------------------------------------------------ //

    private static bool IsNormalStyle(string styleId)
    {
        return string.Equals(styleId, "Normal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(styleId, "Normalny", StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------ //
    //  Rule 3a — Font size
    // ------------------------------------------------------------------ //

    private static void CheckFontSize(
        WordprocessingDocument doc,
        Paragraph caption,
        string? styleId,
        int paraIndex,
        string preview,
        List<ValidationResult> errors)
    {
        var pt = ResolveEffectiveFontSizePt(doc, caption, styleId);
        if (pt is null) return;

        if (Math.Abs(pt.Value - ExpectedFontSizePt) > 0.01)
        {
            errors.Add(MakeResult(
                $"Figure caption font size must be 11pt, found {pt:0.##}pt.",
                paraIndex, preview));
        }
    }

    private static double? ResolveEffectiveFontSizePt(
        WordprocessingDocument doc, Paragraph caption, string? styleId)
    {
        // 1. First text-bearing run with explicit size
        foreach (var run in caption.Elements<Run>())
        {
            if (string.IsNullOrWhiteSpace(GetRunText(run))) continue;
            if (TryParseHalfPts(run.RunProperties?.FontSize?.Val?.Value, out var pt))
                return pt;
        }

        // 2. Paragraph style (walk basedOn chain)
        if (!string.IsNullOrEmpty(styleId))
        {
            var pt = GetFontSizeFromStyleChain(doc, styleId, new HashSet<string>());
            if (pt is not null) return pt;
        }

        // 3. Default paragraph style
        return GetDefaultFontSizePt(doc);
    }

    private static double? GetFontSizeFromStyleChain(
        WordprocessingDocument doc, string styleId, HashSet<string> visited)
    {
        if (!visited.Add(styleId)) return null;

        var style = FindStyle(doc, styleId);
        if (style is null) return null;

        if (TryParseHalfPts(style.StyleRunProperties?.FontSize?.Val?.Value, out var pt))
            return pt;

        var basedOn = style.BasedOn?.Val?.Value;
        return !string.IsNullOrEmpty(basedOn)
            ? GetFontSizeFromStyleChain(doc, basedOn, visited)
            : null;
    }

    private static double? GetDefaultFontSizePt(WordprocessingDocument doc)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var def = styles?.Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.Default?.Value == true);
        return TryParseHalfPts(def?.StyleRunProperties?.FontSize?.Val?.Value, out var pt)
            ? pt : null;
    }

    // ------------------------------------------------------------------ //
    //  Rule 3b — Alignment
    // ------------------------------------------------------------------ //

    private static void CheckAlignment(
        WordprocessingDocument doc,
        Paragraph caption,
        string? styleId,
        int paraIndex,
        string preview,
        List<ValidationResult> errors)
    {
        var jc = ResolveEffectiveJustification(doc, caption, styleId);
        if (jc == JustificationValues.Center) return;

        string name;
        if (jc == JustificationValues.Right)
            name = "right-aligned";
        else if (jc == JustificationValues.Both)
            name = "justified";
        else
            name = "left-aligned";

        errors.Add(MakeResult(
            $"Figure caption must be centered, found {name}.",
            paraIndex, preview));
    }

    private static JustificationValues ResolveEffectiveJustification(
        WordprocessingDocument doc, Paragraph caption, string? styleId)
    {
        var jc = caption.ParagraphProperties?.Justification?.Val?.Value;
        if (jc is not null) return jc.Value;

        if (!string.IsNullOrEmpty(styleId))
        {
            var styleJc = GetJustificationFromStyleChain(doc, styleId, new HashSet<string>());
            if (styleJc is not null) return styleJc.Value;
        }

        return JustificationValues.Left;
    }

    private static JustificationValues? GetJustificationFromStyleChain(
        WordprocessingDocument doc, string styleId, HashSet<string> visited)
    {
        if (!visited.Add(styleId)) return null;

        var style = FindStyle(doc, styleId);
        if (style is null) return null;

        var jc = style.StyleParagraphProperties?.Justification?.Val?.Value;
        if (jc is not null) return jc;

        var basedOn = style.BasedOn?.Val?.Value;
        return !string.IsNullOrEmpty(basedOn)
            ? GetJustificationFromStyleChain(doc, basedOn, visited)
            : null;
    }

    // ------------------------------------------------------------------ //
    //  Rule 3c — Indentation
    // ------------------------------------------------------------------ //

    private static void CheckIndentation(
        WordprocessingDocument doc,
        Paragraph caption,
        string? styleId,
        int paraIndex,
        string preview,
        List<ValidationResult> errors)
    {
        var (left, firstLine) = ResolveEffectiveIndentation(doc, caption, styleId);
        if (Math.Abs(left) <= IndentToleranceTwips && Math.Abs(firstLine) <= IndentToleranceTwips)
            return;

        errors.Add(MakeResult(
            $"Figure caption must have no indentation (left: {left / TwipsPerCm:F2}cm, first-line: {firstLine / TwipsPerCm:F2}cm).",
            paraIndex, preview));
    }

    private static (int left, int firstLine) ResolveEffectiveIndentation(
        WordprocessingDocument doc, Paragraph caption, string? styleId)
    {
        var indent = caption.ParagraphProperties?.Indentation;
        if (indent is not null)
            return ParseIndentation(indent);

        if (!string.IsNullOrEmpty(styleId))
        {
            var style = FindStyle(doc, styleId);
            indent = style?.StyleParagraphProperties?.Indentation;
            if (indent is not null)
                return ParseIndentation(indent);
        }

        return (0, 0);
    }

    private static (int left, int firstLine) ParseIndentation(Indentation indent)
    {
        var left = ParseTwips(indent.Left?.Value);
        var firstLine = ParseTwips(indent.FirstLine?.Value);
        var hanging = ParseTwips(indent.Hanging?.Value);
        // Hanging indent is stored separately; effective first-line = -hanging
        if (hanging != 0 && firstLine == 0)
            firstLine = -hanging;
        return (left, firstLine);
    }

    // ------------------------------------------------------------------ //
    //  Shared helpers
    // ------------------------------------------------------------------ //

    private static Style? FindStyle(WordprocessingDocument doc, string styleId)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        return styles?.Elements<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId, styleId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseHalfPts(string? value, out double points)
    {
        points = 0;
        if (string.IsNullOrEmpty(value)) return false;
        if (!double.TryParse(value, out var hp)) return false;
        points = hp / 2.0;
        return true;
    }

    private static int ParseTwips(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value, out var v) ? v : 0;
    }

    private static ValidationResult MakeResult(string message, int paragraph, string text)
    {
        return new ValidationResult
        {
            RuleName = "FigureCaptionStyleRule",
            Message = message,
            IsError = true,
            Location = new DocumentLocation
            {
                Paragraph = paragraph,
                Text = text
            }
        };
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static string GetRunText(Run run)
    {
        return string.Concat(run.Elements<Text>().Select(t => t.Text));
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        return text[..maxLength] + "...";
    }
}
