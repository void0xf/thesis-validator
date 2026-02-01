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


    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart.Document.Body;
        var paragraphSpacingRuleConfigValue = config.Formatting.Layout.ParagraphSpacingRule;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var spacing = paragraph.ParagraphProperties?.SpacingBetweenLines;
            string afterValue = spacing?.After;

            int spacingAfter = 0;
            if (afterValue != null)
            {
                // Try to parse. If it fails (e.g. "auto"), we might want to flag it or treat as error.
                if (!int.TryParse(afterValue, out spacingAfter))
                {
                    // If we can't parse it (like "auto"), it's likely not 0 or 120 fixed.
                    spacingAfter = -1;
                }
            }

            if (!paragraphSpacingRuleConfigValue.Contains(spacingAfter))
            {
                var errorMessage = $"Paragraph has incorrect spacing or set to auto. After value: {spacingAfter} twips. Expected 0 or 120 (6pt).";
                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = errorMessage,
                    IsError = true,
                });
                documentCommentService.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }
}