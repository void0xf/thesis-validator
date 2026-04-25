using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace Rules;

/// <summary>
/// Rule #15: Standard text must use full justification (both margins).
/// Exceptions: Lists, Headings, Titles, Subtitles, Captions, and TOC entries.
/// </summary>
public class TextJustificationRule : IValidationRule
{
    public string Name => "TextJustificationRule";

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            var text = DocumentAnalysisScope.GetParagraphText(paragraph, config);
            if (!DocumentAnalysisScope.HasMeaningfulContent(text))
                continue;

            // Skip if this is a list item (has numbering properties)
            if (IsListItem(paragraph))
                continue;

            // Skip if this is an excluded style (heading, title, TOC, etc.)
            if (StylePatternExclusionHelper.HasExcludedStyle(paragraph))
                continue;

            // Check justification
            var justification = ResolveEffectiveJustification(doc, paragraph);

            // Full justification = Both (left and right margins)
            if (justification != JustificationValues.Both)
            {
                var alignmentName = GetAlignmentName(justification);
                var preview = Truncate(text, 50);

                var errorMessage = $"Paragraph is {alignmentName} aligned. Standard text must use full justification (both margins).";

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

    private static bool IsListItem(Paragraph paragraph)
    {
        // Check if paragraph has numbering properties (bullet or numbered list)
        return paragraph.ParagraphProperties?.NumberingProperties != null;
    }

    private static JustificationValues ResolveEffectiveJustification(WordprocessingDocument doc, Paragraph paragraph)
    {
        // 1. Check paragraph-level justification
        var paraJustification = paragraph.ParagraphProperties?.Justification?.Val?.Value;
        if (paraJustification.HasValue)
            return paraJustification.Value;

        // 2. Check paragraph style
        var styleJustification = GetStyleJustification(doc, paragraph);
        if (styleJustification.HasValue)
            return styleJustification.Value;

        // 3. Check default paragraph style
        var defaultJustification = GetDefaultJustification(doc);
        if (defaultJustification.HasValue)
            return defaultJustification.Value;

        // 4. Default to Left if nothing is specified
        return JustificationValues.Left;
    }

    private static JustificationValues? GetStyleJustification(WordprocessingDocument doc, Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return null;

        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var style = styles?.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == styleId);

        var justification = style?.StyleParagraphProperties?.Justification?.Val?.Value;
        if (justification.HasValue)
            return justification.Value;

        // Check basedOn style
        var basedOnStyleId = style?.BasedOn?.Val?.Value;
        if (!string.IsNullOrEmpty(basedOnStyleId))
        {
            var basedOnStyle = styles?.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == basedOnStyleId);
            return basedOnStyle?.StyleParagraphProperties?.Justification?.Val?.Value;
        }

        return null;
    }

    private static JustificationValues? GetDefaultJustification(WordprocessingDocument doc)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;

        // Find the default paragraph style
        var defaultStyle = styles?
            .Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.Default?.Value == true);

        return defaultStyle?.StyleParagraphProperties?.Justification?.Val?.Value;
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

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
    }
}
