using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;

namespace backend.Rules;

public class FontFamilyValidationRule : IValidationRule
{
    public string Name => nameof(FontConfig.FontFamily);

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config)
    {
        return Validate(doc, config, null);
    }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? commentService)
    {
        var expectedFont = config.Formatting.Font.FontFamily;
        var errors = new List<ValidationResult>();

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            ValidateParagraph(doc, paragraph, paragraphIndex, expectedFont, config, errors, commentService);
        }

        return errors;
    }

    private void ValidateParagraph(
        WordprocessingDocument doc,
        Paragraph paragraph,
        int paragraphIndex,
        string expectedFont,
        UniversityConfig config,
        List<ValidationResult> errors,
        DocumentCommentService? commentService)
    {
        int runIndex = 0;
        int characterOffset = 0;

        foreach (var run in paragraph.Elements<Run>())
        {
            runIndex++;
            var text = TextExtractionService.GetRunText(run, config);

            if (!string.IsNullOrWhiteSpace(text))
            {
                var actualFont = FormattingResolutionService.ResolveFontFamily(doc, paragraph, run);

                if (!string.Equals(actualFont, expectedFont, StringComparison.OrdinalIgnoreCase))
                {
                    var message = $"Invalid font '{actualFont ?? "unknown"}' found, expected '{expectedFont}'";

                    commentService?.AddCommentToRun(doc, run, message);

                    errors.Add(ValidationResultFactory.ForRun(
                        Name,
                        config,
                        message,
                        paragraphIndex,
                        runIndex,
                        characterOffset,
                        text.Length,
                        TextExtractionService.Truncate(text, 50),
                        ParagraphIndexKind.BodyElement));
                }
            }

            characterOffset += text.Length;
        }
    }
}
