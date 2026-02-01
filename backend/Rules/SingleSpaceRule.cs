using System.Text.RegularExpressions;
using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace Rules;

/// <summary>
/// Rule #13: Only single spaces allowed between words.
/// (Polish: Odstęp między wyrazami jedna spacja)
/// </summary>
public partial class SingleSpaceRule : IValidationRule
{
    public string Name => "SingleSpaceRule";

    // Regex to find 2 or more consecutive spaces
    [GeneratedRegex(@"  +", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
            return errors;

        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;

            var text = GetParagraphText(paragraph);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var matches = MultipleSpacesRegex().Matches(text);

            foreach (Match match in matches)
            {
                var snippet = GetContextSnippet(text, match.Index, match.Length);
                var spaceCount = match.Length;

                var errorMessage = $"Multiple spaces found ({spaceCount} spaces). Only single spaces allowed between words. Context: \"{snippet}\"";

                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = errorMessage,
                    IsError = true,
                    Location = new DocumentLocation
                    {
                        Paragraph = paragraphIndex,
                        CharacterOffset = match.Index,
                        Length = match.Length,
                        Text = snippet
                    }
                });

                documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static string GetContextSnippet(string text, int matchIndex, int matchLength, int contextChars = 15)
    {
        var start = Math.Max(0, matchIndex - contextChars);
        var end = Math.Min(text.Length, matchIndex + matchLength + contextChars);

        var snippet = text[start..end];

        var prefix = start > 0 ? "..." : "";
        var suffix = end < text.Length ? "..." : "";

        var beforeMatch = snippet[..(matchIndex - start)];
        var theMatch = snippet[(matchIndex - start)..(matchIndex - start + matchLength)];
        var afterMatch = snippet[(matchIndex - start + matchLength)..];

        return $"{prefix}{beforeMatch}[{matchLength} spaces]{afterMatch}{suffix}";
    }
}
