using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace Rules;

/// <summary>
/// Hierarchy: The division of the work cannot be deeper than 3 levels.
/// Any usage of Heading 4, Heading 5, etc. is forbidden.
/// </summary>
public class HierarchyDepthRule : IValidationRule
{
    private const int MaxAllowedLevel = 3;

    public string Name => "HierarchyDepthRule";

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService = null)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;

        if (body is null)
            return errors;

        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;

            var level = HeadingStyleHelper.GetHeadingLevel(doc, paragraph);
            if (level is null || level <= MaxAllowedLevel)
                continue;

            var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
            var preview = text.Length > 60 ? text[..60] + "..." : text;

            var errorMessage = $"Structure too deep. Detected Level {level}, but maximum allowed is {MaxAllowedLevel}.";

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

        return errors;
    }
}
