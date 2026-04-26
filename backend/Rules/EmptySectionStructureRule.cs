using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Skipping;
using backend.Services.Structure;

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
        int childIndex = 0;
        var firstIncludedChildIndex = DocumentAnalysisScope.GetFirstIncludedBodyChildIndex(doc, config);
        var tocParagraphs = TableOfContentsSkipRule.GetSkippedParagraphs(doc, config);

        foreach (var element in body.ChildElements)
        {
            if (element is Paragraph)
                paragraphIndex++;

            if (childIndex++ < firstIncludedChildIndex)
                continue;

            // Tables, SdtBlocks, etc. count as body content.
            if (element is not Paragraph paragraph)
            {
                if (lastHeadingLevel is not null)
                    hasBodyContentSinceHeading = true;
                continue;
            }

            var skipDecision = SkipDecisionService.ShouldSkipParagraph(
                doc,
                paragraph,
                config,
                new SkipContext(paragraphIndex, null, tocParagraphs));
            if (skipDecision.ShouldSkip)
                continue;

            var level = HeadingDetectionService.GetHeadingLevel(doc, paragraph);

            if (level is not null)
            {
                // Current element is a heading.
                if (lastHeadingLevel is not null
                    && level > lastHeadingLevel
                    && !hasBodyContentSinceHeading)
                {
                    var currentText = TextExtractionService.Truncate(
                        TextExtractionService.GetParagraphText(doc, paragraph, config).Trim(),
                        50);

                    var msg =
                        $"Heading {lastHeadingLevel} \"{lastHeadingPreview}\" " +
                        $"is immediately followed by Heading {level} \"{currentText}\" " +
                        "with no introductory text. Add at least one paragraph of body text " +
                        "before the first sub-section.";

                    errors.Add(ValidationResultFactory.ForParagraph(
                        Name,
                        config,
                        msg,
                        lastHeadingParaIdx,
                        lastHeadingPreview,
                        ParagraphIndexKind.BodyElement));

                    if (lastHeadingParagraph is not null)
                        commentService?.AddCommentToParagraph(doc, lastHeadingParagraph, msg);
                }

                lastHeadingLevel = level;
                lastHeadingParaIdx = paragraphIndex;
                lastHeadingPreview = TextExtractionService.Truncate(
                    TextExtractionService.GetParagraphText(doc, paragraph, config).Trim(),
                    60);
                lastHeadingParagraph = paragraph;
                hasBodyContentSinceHeading = false;
            }
            else
            {
                // Non-heading paragraph — any visible text counts as body content.
                if (!hasBodyContentSinceHeading
                    && !string.IsNullOrWhiteSpace(TextExtractionService.GetParagraphText(doc, paragraph, config)))
                {
                    hasBodyContentSinceHeading = true;
                }
            }
        }

        return errors;
    }
}
