using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace Rules;

public class ParagraphSpacingRule : IValidationRule
{
    public string Name => nameof(LayoutConfig.ParagraphSpacingRule);

    // 1 point = 20 twips
    private const int TwipsPerPoint = 20;

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var allowedSpacingTwips = config.Formatting.Layout.ParagraphSpacingRule
            .Select(pt => pt * TwipsPerPoint)
            .ToHashSet();

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            if (HeadingStyleHelper.IsHeading(doc, paragraph))
                continue;

            if (StylePatternExclusionHelper.HasExcludedStyle(paragraph))
                continue;

            var spacingAfter = ResolveEffectiveSpacingAfter(doc, paragraph);
            var preview = GetParagraphPreview(paragraph, config, 50);

            if (!allowedSpacingTwips.Contains(spacingAfter) 
                && !string.IsNullOrEmpty(preview) 
                && !string.IsNullOrWhiteSpace(preview))
            {
                var expectedPts = string.Join(" or ", config.Formatting.Layout.ParagraphSpacingRule.Select(pt => $"{pt}pt"));
                var actualPt = spacingAfter / (double)TwipsPerPoint;
                var errorMessage = $"Paragraph has incorrect spacing. After value: {actualPt:F1}pt ({spacingAfter} twips). Expected {expectedPts}.";
                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = errorMessage,
                    IsError = true,
                    Location = new DocumentLocation
                    {
                        Paragraph = paragraphIndex,
                        Text = preview
                    }
                });
                documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static int ResolveEffectiveSpacingAfter(WordprocessingDocument doc, Paragraph paragraph)
    {
        var directAfter = ParseSpacingValue(paragraph.ParagraphProperties?.SpacingBetweenLines?.After?.Value);
        if (directAfter.HasValue)
            return directAfter.Value;

        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (!string.IsNullOrEmpty(styleId))
        {
            var styleAfter = GetSpacingAfterFromStyleChain(doc, styleId, new HashSet<string>());
            if (styleAfter.HasValue)
                return styleAfter.Value;
        }

        var defaultStyleAfter = GetDefaultParagraphStyleSpacingAfter(doc);
        if (defaultStyleAfter.HasValue)
            return defaultStyleAfter.Value;

        var docDefaults = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .DocDefaults?
            .ParagraphPropertiesDefault?
            .ParagraphPropertiesBaseStyle?
            .SpacingBetweenLines;
        var docDefaultAfter = ParseSpacingValue(docDefaults?.After?.Value);
        return docDefaultAfter ?? 0;
    }

    private static int? GetSpacingAfterFromStyleChain(
        WordprocessingDocument doc,
        string styleId,
        HashSet<string> visited)
    {
        if (string.IsNullOrEmpty(styleId) || !visited.Add(styleId))
            return null;

        var style = FindStyle(doc, styleId);
        if (style is null)
            return null;

        var spacingAfter = ParseSpacingValue(style.StyleParagraphProperties?.SpacingBetweenLines?.After?.Value);
        if (spacingAfter.HasValue)
            return spacingAfter.Value;

        var basedOnStyleId = style.BasedOn?.Val?.Value;
        return string.IsNullOrEmpty(basedOnStyleId)
            ? null
            : GetSpacingAfterFromStyleChain(doc, basedOnStyleId, visited);
    }

    private static int? GetDefaultParagraphStyleSpacingAfter(WordprocessingDocument doc)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var defaultStyle = styles?
            .Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.Default?.Value == true);

        if (defaultStyle is null)
            return null;

        var spacingAfter = ParseSpacingValue(defaultStyle.StyleParagraphProperties?.SpacingBetweenLines?.After?.Value);
        if (spacingAfter.HasValue)
            return spacingAfter.Value;

        var basedOnStyleId = defaultStyle.BasedOn?.Val?.Value;
        return string.IsNullOrEmpty(basedOnStyleId)
            ? null
            : GetSpacingAfterFromStyleChain(doc, basedOnStyleId, new HashSet<string>());
    }

    private static Style? FindStyle(WordprocessingDocument doc, string styleId)
    {
        return doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
            .Elements<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId?.Value, styleId, StringComparison.OrdinalIgnoreCase));
    }

    private static int? ParseSpacingValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return int.TryParse(value, out var spacing)
            ? spacing
            : -1;
    }

    private static string GetParagraphPreview(Paragraph paragraph, UniversityConfig config, int maxLength)
    {
        var raw = DocumentAnalysisScope.GetParagraphText(paragraph, config);
        var clean = SanitizePreview(raw);
        if (string.IsNullOrEmpty(clean)) return string.Empty;
        return clean.Length <= maxLength ? clean : clean[..maxLength] + "...";
    }

    /// <summary>
    /// Strips invisible / non-printable characters that render as squares in monospace fonts.
    /// </summary>
    private static string SanitizePreview(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch == '\u00A0') { sb.Append(' '); continue; }   // non-breaking space → regular space
            if (char.IsControl(ch)) continue;                    // strip control chars
            if (char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.Format) continue; // strip zero-width/soft-hyphen etc.
            sb.Append(ch);
        }
        return sb.ToString().Trim();
    }
}
