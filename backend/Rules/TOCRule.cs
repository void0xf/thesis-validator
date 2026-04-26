using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Structure;

namespace Rules;

public class TocRule : IValidationRule
{
    public const string ManualTableOfContentsRuleName = "Manual table of contents";

    private const string ManualTableOfContentsMessage =
        "A table of contents section was detected, but no automatic Word TOC field was found. The table of contents was probably created manually and may become inconsistent with the document structure.";

    public string Name => nameof(FormattingConfig.CheckTableOfContents);

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        var errors = new List<ValidationResult>();
        var detection = DetectTableOfContents(doc);

        if (detection.Kind == TableOfContentsKind.Automatic)
            return errors;

        if (detection.Kind == TableOfContentsKind.Manual)
        {
            errors.Add(ValidationResultFactory.ForParagraph(
                ManualTableOfContentsRuleName,
                config,
                ManualTableOfContentsMessage,
                detection.ParagraphIndex,
                TextExtractionService.Truncate(detection.Text ?? string.Empty, 80),
                severity: ValidationSeverity.Warning));

            if (detection.Paragraph is not null && documentCommentService is not null)
                documentCommentService.AddCommentToParagraph(doc, detection.Paragraph, ManualTableOfContentsMessage);

            return errors;
        }

        var body = doc.MainDocumentPart?.Document.Body;
        var firstRun = body?.Descendants<Run>().FirstOrDefault();
        errors.Add(ValidationResultFactory.Create(
            Name,
            config,
            "Document is missing a Table of Contents."));

        if (firstRun != null && documentCommentService != null)
            documentCommentService.AddCommentToRun(doc, firstRun, "Document is missing a Table of Contents");

        return errors;
    }

    public static TableOfContentsDetection DetectTableOfContents(WordprocessingDocument doc)
    {
        return TableOfContentsDetectionService.Detect(doc);
    }

    public static bool IsTableOfContentsHeadingText(string text)
    {
        return TableOfContentsDetectionService.IsTableOfContentsHeadingText(text);
    }
}
