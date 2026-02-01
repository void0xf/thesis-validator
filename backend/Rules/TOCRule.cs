using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using ThesisValidator.Rules;

namespace Rules;

public class TocRule : IValidationRule
{
    public string Name => nameof(FormattingConfig.CheckTableOfContents);

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        var body = doc.MainDocumentPart!.Document.Body!;
        var errors = new List<ValidationResult>();
        bool tocExists = body.Descendants<FieldCode>().Any(i => i.Text.Trim().StartsWith("TOC"));

        if (!tocExists)
        {
            Run firstRun = body.Descendants<Run>().FirstOrDefault();
            errors.Add(new ValidationResult
            {
                RuleName = Name,
                Message = "Document is missing a Table of Contents.",
                IsError = true
            });
            if (firstRun != null && documentCommentService != null)
                documentCommentService.AddCommentToRun(doc, firstRun, "Document is missing a Table of Contents");
        }

        return errors;
    }
}