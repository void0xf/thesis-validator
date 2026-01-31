using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

public class FontFamilyValidationRule : IValidationRule
{
    public string Name => nameof(FontConfig.FontFamily);

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config)
    {
        return Validate(doc, config, null);
    }

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? commentService)
    {
        var expectedFont = config.Formatting.Font.FontFamily;
        var body = doc.MainDocumentPart!.Document.Body!;
        var errors = new List<ValidationResult>();

        int paragraphIndex = 0;
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;
            ValidateParagraph(doc, paragraph, paragraphIndex, expectedFont, errors, commentService);
        }

        return errors;
    }

    private void ValidateParagraph(
        WordprocessingDocument doc,
        Paragraph paragraph,
        int paragraphIndex,
        string expectedFont,
        List<ValidationResult> errors,
        DocumentCommentService? commentService)
    {
        int runIndex = 0;
        int characterOffset = 0;

        foreach (var run in paragraph.Elements<Run>())
        {
            runIndex++;
            var text = GetRunText(run);

            if (!string.IsNullOrWhiteSpace(text))
            {
                var actualFont = ResolveEffectiveFont(doc, paragraph, run);

                if (!string.Equals(actualFont, expectedFont, StringComparison.OrdinalIgnoreCase))
                {
                    var message = $"Invalid font '{actualFont ?? "unknown"}' found, expected '{expectedFont}'";

                    commentService?.AddCommentToRun(doc, run, message);

                    errors.Add(new ValidationResult
                    {
                        RuleName = Name,
                        IsError = true,
                        Message = message,
                        Location = new DocumentLocation
                        {
                            Paragraph = paragraphIndex,
                            Run = runIndex,
                            CharacterOffset = characterOffset,
                            Length = text.Length,
                            Text = Truncate(text, 50)
                        }
                    });
                }
            }

            characterOffset += text.Length;
        }
    }

    private static string GetRunText(Run run)
    {
        return string.Concat(run.Elements<Text>().Select(t => t.Text));
    }

    private static string? ResolveEffectiveFont(
        WordprocessingDocument doc,
        Paragraph paragraph,
        Run run)
    {
        var runFont = run.RunProperties?.RunFonts?.Ascii;
        if (!string.IsNullOrEmpty(runFont))
            return runFont;

        var paraFont = GetParagraphStyleFont(doc, paragraph);
        if (!string.IsNullOrEmpty(paraFont))
            return paraFont;

        return GetDefaultFont(doc);
    }

    private static string? GetParagraphStyleFont(
        WordprocessingDocument doc,
        Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val;
        if (styleId == null)
            return null;

        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var style = styles?.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);

        return style?.StyleRunProperties?.RunFonts?.Ascii;
    }

    private static string? GetDefaultFont(WordprocessingDocument doc)
    {
        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        var defaultStyle = styles?
            .Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Paragraph && s.Default?.Value == true);

        return defaultStyle?.StyleRunProperties?.RunFonts?.Ascii;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
    }
}