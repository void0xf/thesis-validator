using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// A subchapter heading (e.g. Heading 2) cannot immediately follow its parent
/// chapter heading (e.g. Heading 1) without any intervening body text.
/// Every section must contain at least a brief introductory paragraph
/// before the first sub-section begins.
/// </summary>
public class EmptySectionStructureRule : IValidationRule
{
    public string Name => "EmptySectionStructureRule";

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService = null)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null) return errors;

        int? lastHeadingLevel = null;
        int lastHeadingParaIdx = 0;
        string lastHeadingPreview = "";
        Paragraph? lastHeadingParagraph = null;
        bool hasBodyContentSinceHeading = false;

        int paragraphIndex = 0;

        foreach (var element in body.ChildElements)
        {
            // Tables, SdtBlocks, etc. count as body content.
            if (element is not Paragraph paragraph)
            {
                if (lastHeadingLevel is not null)
                    hasBodyContentSinceHeading = true;
                continue;
            }

            paragraphIndex++;

            var level = HeadingStyleHelper.GetHeadingLevel(doc, paragraph);

            if (level is not null)
            {
                // Current element is a heading.
                if (lastHeadingLevel is not null
                    && level > lastHeadingLevel
                    && !hasBodyContentSinceHeading)
                {
                    var currentText = Truncate(GetParagraphText(paragraph).Trim(), 50);

                    var msg =
                        $"Heading {lastHeadingLevel} \"{lastHeadingPreview}\" " +
                        $"is immediately followed by Heading {level} \"{currentText}\" " +
                        "with no introductory text. Add at least one paragraph of body text " +
                        "before the first sub-section.";

                    errors.Add(new ValidationResult
                    {
                        RuleName = Name,
                        Message = msg,
                        IsError = true,
                        Location = new DocumentLocation
                        {
                            Paragraph = lastHeadingParaIdx,
                            Text = lastHeadingPreview
                        }
                    });

                    if (lastHeadingParagraph is not null)
                        commentService?.AddCommentToParagraph(doc, lastHeadingParagraph, msg);
                }

                lastHeadingLevel = level;
                lastHeadingParaIdx = paragraphIndex;
                lastHeadingPreview = Truncate(GetParagraphText(paragraph).Trim(), 60);
                lastHeadingParagraph = paragraph;
                hasBodyContentSinceHeading = false;
            }
            else
            {
                // Non-heading paragraph — any visible text counts as body content.
                if (!hasBodyContentSinceHeading
                    && !string.IsNullOrWhiteSpace(GetParagraphText(paragraph)))
                {
                    hasBodyContentSinceHeading = true;
                }
            }
        }

        return errors;
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        return text[..maxLength] + "...";
    }
}
