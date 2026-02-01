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
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
            return errors;

        var allowedSpacingTwips = config.Formatting.Layout.ParagraphSpacingRule
            .Select(pt => pt * TwipsPerPoint)
            .ToHashSet();

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
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

            if (!allowedSpacingTwips.Contains(spacingAfter))
            {
                var expectedPts = string.Join(" or ", config.Formatting.Layout.ParagraphSpacingRule.Select(pt => $"{pt}pt"));
                var actualPt = spacingAfter / (double)TwipsPerPoint;
                var errorMessage = $"Paragraph has incorrect spacing. After value: {actualPt:F1}pt ({spacingAfter} twips). Expected {expectedPts}.";
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
}