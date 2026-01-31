using backend.Models;
using backend.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using ThesisValidator.Rules;

namespace backend.Services;

public class ThesisValidatorService(IEnumerable<IValidationRule> rules)
{
    private readonly IReadOnlyList<IValidationRule> _ruleList = rules.ToList();

    public IEnumerable<ValidationResult> Validate(Stream fileStream, UniversityConfig config)
    {
        var doc = WordprocessingDocument.Open(fileStream, false);

        var errors = new List<ValidationResult>();
        foreach (var rule in _ruleList)
        {
            errors.AddRange(rule.Validate(doc, config));
        }

        return errors;
    }

    /// <summary>
    /// Validates the document and adds comments for each error found.
    /// Returns both the validation results and a stream containing the annotated document.
    /// </summary>
    public (IEnumerable<ValidationResult> Results, MemoryStream AnnotatedDocument) ValidateWithComments(Stream fileStream, UniversityConfig config)
    {
        var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var doc = WordprocessingDocument.Open(memoryStream, true);
        var commentService = new DocumentCommentService();

        var errors = new List<ValidationResult>();
        foreach (var rule in _ruleList)
        {
            errors.AddRange(rule.Validate(doc, config, commentService));
        }

        doc.MainDocumentPart?.Document.Save();
        var annotatedStream = DocumentCommentService.SaveDocumentWithComments(doc);

        return (errors, annotatedStream);
    }
}