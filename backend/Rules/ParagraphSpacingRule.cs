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

            var spacing = paragraph.ParagraphProperties?.SpacingBetweenLines;
            var afterValue = spacing?.After?.Value;

            int spacingAfter = 0;
            if (afterValue != null)
            {
                // Try to parse. If it fails (e.g. "auto"), we might want to flag it or treat as error.
                if (!int.TryParse(afterValue, out spacingAfter))
                {
                    spacingAfter = -1;
                }
            }
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
