using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Structure;

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
        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            var level = HeadingDetectionService.GetHeadingLevel(doc, paragraph);
            if (level is null || level <= MaxAllowedLevel)
                continue;

            var text = TextExtractionService.GetParagraphText(doc, paragraph, config);
            var preview = TextExtractionService.Truncate(text, 60);

            var errorMessage = $"Structure too deep. Detected Level {level}, but maximum allowed is {MaxAllowedLevel}.";

            errors.Add(ValidationResultFactory.ForParagraph(
                Name,
                config,
                errorMessage,
                paragraphIndex,
                preview));

            documentCommentService?.AddCommentToParagraph(doc, paragraph, errorMessage);
        }

        return errors;
    }
}
