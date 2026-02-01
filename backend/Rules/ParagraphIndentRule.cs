using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

public class ParagraphIndentRule : IValidationRule
{
    public string Name => nameof(LayoutConfig.RequiredIndentCm);

    // 1 cm = 567 twips (twentieths of a point)
    private const double TwipsPerCm = 567.0;

    // Tolerance for comparison - about 0.1 cm or ~57 twips
    // This accounts for rounding differences between metric and imperial units
    private const int ToleranceTwips = 60;

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
            return errors;

        // Allowed indents: 1 cm (~567 twips) or 1.25 cm (~709 twips)
        // Word may store as 567, 568, 708, 709, 720 depending on rounding
        // Using explicit twip values that Word commonly uses
        var allowedIndentsTwips = new[] { 567, 709 }; // 1 cm and 1.25 cm

        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;

            if (!HasTextContent(paragraph))
                continue;

            if (IsHeadingOrSpecialParagraph(doc, paragraph))
                continue;

            if (IsCenteredOrRightAligned(doc, paragraph))
                continue;

            var firstLineIndent = GetEffectiveFirstLineIndent(doc, paragraph);
            var startsWithTab = StartsWithTabCharacter(paragraph);

            if (firstLineIndent == 0 && IsListItem(paragraph))
                continue;

            if (startsWithTab && firstLineIndent == 0)
            {
                var message = "Paragraph uses TAB character for indent instead of proper first-line indent formatting. Please use paragraph formatting (1.00 cm or 1.25 cm first-line indent) instead of TAB.";

                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = message,
                    IsError = true,
                    Location = new DocumentLocation
                    {
                        Paragraph = paragraphIndex,
                        Text = GetParagraphPreview(paragraph, 50)
                    }
                });

                documentCommentService?.AddCommentToParagraph(doc, paragraph, message);
                continue;
            }

            if (!IsValidIndent(firstLineIndent, allowedIndentsTwips))
            {
                var actualIndentCm = firstLineIndent / TwipsPerCm;
                var message = $"Paragraph has incorrect first line indent: {actualIndentCm:F2} cm. Expected 1.00 cm or 1.25 cm.";

                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = message,
                    IsError = true,
                    Location = new DocumentLocation
                    {
                        Paragraph = paragraphIndex,
                        Text = GetParagraphPreview(paragraph, 50)
                    }
                });

                documentCommentService?.AddCommentToParagraph(doc, paragraph, message);
            }
        }

        return errors;
    }

    private static int GetFirstLineIndent(Indentation? indentation)
    {
        if (indentation == null)
            return 0;

        var firstLineValue = indentation.FirstLine?.Value;
        var hangingValue = indentation.Hanging?.Value;
        var firstLineCharsValue = indentation.FirstLineChars?.Value;

        if (!string.IsNullOrEmpty(firstLineValue) && int.TryParse(firstLineValue, out var firstLine))
        {
            return firstLine;
        }

        // FirstLineChars is in 1/100ths of a character width - approximate as ~2.5 twips per unit
        if (firstLineCharsValue.HasValue)
        {
            return (int)(firstLineCharsValue.Value * 2.5);
        }

        if (!string.IsNullOrEmpty(hangingValue) && int.TryParse(hangingValue, out var hanging))
        {
            return -hanging;
        }

        return 0;
    }

    /// <summary>
    /// Checks if indentation is explicitly set on this element (even if set to 0).
    /// This is different from "not set at all" (which means inherit).
    /// </summary>
    private static bool HasExplicitIndentation(Indentation? indentation)
    {
        if (indentation == null)
            return false;

        // If any first-line related attribute exists, it's explicitly set
        return indentation.FirstLine != null ||
               indentation.Hanging != null ||
               indentation.FirstLineChars != null;
    }

    /// <summary>
    /// Gets the effective first-line indent by checking:
    /// 1. Direct paragraph properties (if explicitly set)
    /// 2. Paragraph style properties
    /// 3. Default paragraph style (e.g., "Normal", "Normalny")
    /// 4. Document defaults
    /// </summary>
    private static int GetEffectiveFirstLineIndent(WordprocessingDocument doc, Paragraph paragraph)
    {
        var pPr = paragraph.ParagraphProperties;

        if (pPr != null)
        {
            var directIndentation = pPr.Indentation;
            if (HasExplicitIndentation(directIndentation))
            {
                return GetFirstLineIndent(directIndentation);
            }
        }

        var styleId = pPr?.ParagraphStyleId?.Val?.Value;
        if (!string.IsNullOrEmpty(styleId))
        {
            var styleIndent = GetIndentFromStyleChain(doc, styleId, new HashSet<string>());
            if (styleIndent.HasValue)
                return styleIndent.Value;
        }

        var defaultParaStyleIndent = GetIndentFromDefaultParagraphStyle(doc);
        if (defaultParaStyleIndent.HasValue)
            return defaultParaStyleIndent.Value;

        var docDefaults = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?.DocDefaults;
        var defaultIndentation = docDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle?.Indentation;
        if (HasExplicitIndentation(defaultIndentation))
        {
            return GetFirstLineIndent(defaultIndentation);
        }

        return 0;
    }

    /// <summary>
    /// Gets the indent from the default paragraph style (style with Default=true).
    /// Handles localized names like "Normalny" (Polish), "Standard" (German), etc.
    /// </summary>
    private static int? GetIndentFromDefaultParagraphStyle(WordprocessingDocument doc)
    {
        var stylePart = doc.MainDocumentPart?.StyleDefinitionsPart;
        if (stylePart == null)
            return null;

        var defaultParaStyle = stylePart.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.Default?.Value == true);

        if (defaultParaStyle == null)
            return null;

        var styleIndentation = defaultParaStyle.StyleParagraphProperties?.Indentation;
        if (HasExplicitIndentation(styleIndentation))
        {
            return GetFirstLineIndent(styleIndentation);
        }

        var basedOnStyleId = defaultParaStyle.BasedOn?.Val?.Value;
        if (!string.IsNullOrEmpty(basedOnStyleId))
        {
            return GetIndentFromStyleChain(doc, basedOnStyleId, new HashSet<string>());
        }

        return null;
    }

    /// <summary>
    /// Gets indent from a style, walking up the inheritance chain.
    /// Uses visited set to prevent infinite loops from circular references.
    /// </summary>
    private static int? GetIndentFromStyleChain(WordprocessingDocument doc, string styleId, HashSet<string> visited)
    {
        if (string.IsNullOrEmpty(styleId) || visited.Contains(styleId))
            return null;

        visited.Add(styleId);

        var stylePart = doc.MainDocumentPart?.StyleDefinitionsPart;
        if (stylePart == null)
            return null;

        var style = stylePart.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.StyleId?.Value == styleId);

        if (style == null)
            return null;

        var styleIndentation = style.StyleParagraphProperties?.Indentation;
        if (HasExplicitIndentation(styleIndentation))
        {
            return GetFirstLineIndent(styleIndentation);
        }

        var basedOnStyleId = style.BasedOn?.Val?.Value;
        if (!string.IsNullOrEmpty(basedOnStyleId))
        {
            return GetIndentFromStyleChain(doc, basedOnStyleId, visited);
        }

        return null;
    }

    private bool IsValidIndent(int actualTwips, int[] allowedTwips)
    {
        return allowedTwips.Any(allowed => Math.Abs(actualTwips - allowed) <= ToleranceTwips);
    }

    /// <summary>
    /// Checks if the paragraph starts with a Tab character, which is often used
    /// as a visual indent instead of proper first-line indent formatting.
    /// </summary>
    private static bool StartsWithTabCharacter(Paragraph paragraph)
    {
        var firstRun = paragraph.Elements<Run>().FirstOrDefault();
        if (firstRun == null)
            return false;

        var firstChild = firstRun.Elements().FirstOrDefault();
        if (firstChild is TabChar)
            return true;

        foreach (var child in firstRun.Elements())
        {
            if (child is TabChar)
                return true;
            if (child is Text)
                break; // Stop if we hit text first
        }

        return false;
    }

    private static bool HasTextContent(Paragraph paragraph)
    {
        var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
        return !string.IsNullOrWhiteSpace(text);
    }

    private static bool IsCenteredOrRightAligned(WordprocessingDocument doc, Paragraph paragraph)
    {
        var justification = paragraph.ParagraphProperties?.Justification?.Val?.Value;
        if (justification == JustificationValues.Center || justification == JustificationValues.Right)
            return true;

        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (!string.IsNullOrEmpty(styleId))
        {
            var stylePart = doc.MainDocumentPart?.StyleDefinitionsPart;
            var style = stylePart?.Styles?.Elements<Style>()
                .FirstOrDefault(s => s.StyleId?.Value == styleId);

            var styleJustification = style?.StyleParagraphProperties?.Justification?.Val?.Value;
            if (styleJustification == JustificationValues.Center || styleJustification == JustificationValues.Right)
                return true;
        }

        return false;
    }

    private static bool IsListItem(Paragraph paragraph)
    {
        var numPr = paragraph.ParagraphProperties?.NumberingProperties;
        return numPr != null;
    }

    private static bool IsHeadingOrSpecialParagraph(WordprocessingDocument doc, Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

        if (string.IsNullOrEmpty(styleId))
            return false;

        var stylePart = doc.MainDocumentPart?.StyleDefinitionsPart;
        if (stylePart == null)
            return false;

        var style = stylePart.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.StyleId?.Value == styleId);

        if (style == null)
            return false;

        var styleName = style.StyleName?.Val?.Value?.ToLowerInvariant() ?? "";
        if (styleName.Contains("heading") || styleName.Contains("title") ||
            styleName.Contains("toc") || styleName.Contains("contents") ||
            styleName.Contains("caption") || styleName.Contains("figure") ||
            styleName.Contains("table") || styleName.Contains("bibliography") ||
            styleName.Contains("list"))
        {
            return true;
        }

        var outlineLevel = style.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
        if (outlineLevel.HasValue && outlineLevel.Value <= 8)
        {
            return true;
        }

        return false;
    }

    private static string GetParagraphPreview(Paragraph paragraph, int maxLength)
    {
        var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
        if (text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength) + "...";
    }
}
