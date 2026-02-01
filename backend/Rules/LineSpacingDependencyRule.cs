using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace Rules;

/// <summary>
/// Rule #11: If line spacing is 1.5, then paragraph spacing before and after must be 0.
/// (Polish: Odstępy pomiędzy linijkami 0 przy interlinii 1,5)
/// </summary>
public class LineSpacingDependencyRule : IValidationRule
{
    public string Name => "LineSpacingDependencyRule";

    // Line spacing 1.5 = 240 * 1.5 = 360 (in 240ths of a line)
    private const int LineSpacing15 = 360;

    // 1 point = 20 twips
    private const int TwipsPerPoint = 20;

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
            return errors;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            // Resolve effective line spacing (from paragraph, style, or default)
            var (lineSpacing, lineRule) = ResolveEffectiveLineSpacing(doc, paragraph);

            // Check if line spacing is 1.5 lines
            if (!IsLineSpacing15(lineSpacing, lineRule))
                continue;

            // Resolve effective paragraph spacing (before/after)
            var (spacingBefore, spacingAfter) = ResolveEffectiveParagraphSpacing(doc, paragraph);

            if (spacingBefore != 0 || spacingAfter != 0)
            {
                var beforePt = spacingBefore / (double)TwipsPerPoint;
                var afterPt = spacingAfter / (double)TwipsPerPoint;

                var errorMessage = $"Paragraph with 1.5 line spacing must have 0pt spacing before and after. " +
                                   $"Found: Before={beforePt:F1}pt, After={afterPt:F1}pt.";

                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = errorMessage,
                    IsError = true,
                });

                documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static (int? lineSpacing, LineSpacingRuleValues? lineRule) ResolveEffectiveLineSpacing(
        WordprocessingDocument doc, Paragraph paragraph)
    {
        // 1. Check paragraph-level spacing
        var paraSpacing = paragraph.ParagraphProperties?.SpacingBetweenLines;
        if (paraSpacing?.Line?.Value != null)
        {
            int.TryParse(paraSpacing.Line.Value, out var lineValue);
            return (lineValue, paraSpacing.LineRule?.Value);
        }

        // 2. Check paragraph style
        var styleSpacing = GetStyleLineSpacing(doc, paragraph);
        if (styleSpacing.lineSpacing.HasValue)
            return styleSpacing;

        // 3. Check default paragraph style
        return GetDefaultLineSpacing(doc);
    }

    private static (int? lineSpacing, LineSpacingRuleValues? lineRule) GetStyleLineSpacing(
        WordprocessingDocument doc, Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return (null, null);

        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var style = styles?.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == styleId);

        var spacing = style?.StyleParagraphProperties?.SpacingBetweenLines;
        if (spacing?.Line?.Value != null && int.TryParse(spacing.Line.Value, out var lineValue))
        {
            return (lineValue, spacing.LineRule?.Value);
        }

        // Check if style has a basedOn style
        var basedOnStyleId = style?.BasedOn?.Val?.Value;
        if (!string.IsNullOrEmpty(basedOnStyleId))
        {
            var basedOnStyle = styles?.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == basedOnStyleId);
            var basedOnSpacing = basedOnStyle?.StyleParagraphProperties?.SpacingBetweenLines;
            if (basedOnSpacing?.Line?.Value != null && int.TryParse(basedOnSpacing.Line.Value, out var basedOnLineValue))
            {
                return (basedOnLineValue, basedOnSpacing.LineRule?.Value);
            }
        }

        return (null, null);
    }

    private static (int? lineSpacing, LineSpacingRuleValues? lineRule) GetDefaultLineSpacing(WordprocessingDocument doc)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;

        // Find the default paragraph style
        var defaultStyle = styles?
            .Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.Default?.Value == true);

        var spacing = defaultStyle?.StyleParagraphProperties?.SpacingBetweenLines;
        if (spacing?.Line?.Value != null && int.TryParse(spacing.Line.Value, out var lineValue))
        {
            return (lineValue, spacing.LineRule?.Value);
        }

        // Also check document defaults
        var docDefaults = styles?.DocDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle?.SpacingBetweenLines;
        if (docDefaults?.Line?.Value != null && int.TryParse(docDefaults.Line.Value, out var defaultLineValue))
        {
            return (defaultLineValue, docDefaults.LineRule?.Value);
        }

        return (null, null);
    }

    private static (int spacingBefore, int spacingAfter) ResolveEffectiveParagraphSpacing(
        WordprocessingDocument doc, Paragraph paragraph)
    {
        // 1. Check paragraph-level spacing
        var paraSpacing = paragraph.ParagraphProperties?.SpacingBetweenLines;
        int? before = ParseSpacingValue(paraSpacing?.Before?.Value);
        int? after = ParseSpacingValue(paraSpacing?.After?.Value);

        // 2. If not set, check style
        if (!before.HasValue || !after.HasValue)
        {
            var (styleBefore, styleAfter) = GetStyleParagraphSpacing(doc, paragraph);
            before ??= styleBefore;
            after ??= styleAfter;
        }

        // 3. If still not set, check default style
        if (!before.HasValue || !after.HasValue)
        {
            var (defaultBefore, defaultAfter) = GetDefaultParagraphSpacing(doc);
            before ??= defaultBefore;
            after ??= defaultAfter;
        }

        return (before ?? 0, after ?? 0);
    }

    private static (int? before, int? after) GetStyleParagraphSpacing(
        WordprocessingDocument doc, Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return (null, null);

        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var style = styles?.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == styleId);

        var spacing = style?.StyleParagraphProperties?.SpacingBetweenLines;
        return (ParseSpacingValue(spacing?.Before?.Value), ParseSpacingValue(spacing?.After?.Value));
    }

    private static (int? before, int? after) GetDefaultParagraphSpacing(WordprocessingDocument doc)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;

        var defaultStyle = styles?
            .Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.Default?.Value == true);

        var spacing = defaultStyle?.StyleParagraphProperties?.SpacingBetweenLines;
        return (ParseSpacingValue(spacing?.Before?.Value), ParseSpacingValue(spacing?.After?.Value));
    }

    private static bool IsLineSpacing15(int? lineSpacing, LineSpacingRuleValues? lineRule)
    {
        if (!lineSpacing.HasValue)
            return false;

        // LineRule can be "auto" (default), "exact", or "atLeast"
        // For "auto" (or not specified), the value is in 240ths of a line (1.5 lines = 360)
        if (lineRule == null || lineRule == LineSpacingRuleValues.Auto)
        {
            return lineSpacing.Value == LineSpacing15;
        }

        return false;
    }

    private static int? ParseSpacingValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (int.TryParse(value, out var result))
            return result;

        return null;
    }
}
