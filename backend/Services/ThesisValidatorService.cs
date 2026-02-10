using backend.Models;
using backend.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using ThesisValidator.Rules;

namespace backend.Services;

public class ThesisValidatorService(IEnumerable<IValidationRule> rules)
{
    private readonly IReadOnlyList<IValidationRule> _ruleList = rules.ToList();

    public IEnumerable<ValidationResult> Validate(Stream fileStream, UniversityConfig config, IEnumerable<string>? selectedRules = null)
    {
        var doc = WordprocessingDocument.Open(fileStream, false);
        var rulesToRun = FilterRules(selectedRules);

        var errors = new List<ValidationResult>();
        foreach (var rule in rulesToRun)
        {
            errors.AddRange(rule.Validate(doc, config));
        }

        return errors;
    }

    /// <summary>
    /// Validates the document and adds comments for each error found.
    /// Returns both the validation results and a stream containing the annotated document.
    /// </summary>
    public (IEnumerable<ValidationResult> Results, MemoryStream AnnotatedDocument) ValidateWithComments(Stream fileStream, UniversityConfig config, IEnumerable<string>? selectedRules = null)
    {
        var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var doc = WordprocessingDocument.Open(memoryStream, true);
        var commentService = new DocumentCommentService();
        var rulesToRun = FilterRules(selectedRules);

        var errors = new List<ValidationResult>();
        foreach (var rule in rulesToRun)
        {
            errors.AddRange(rule.Validate(doc, config, commentService));
        }

        doc.MainDocumentPart?.Document.Save();
        var annotatedStream = DocumentCommentService.SaveDocumentWithComments(doc);

        return (errors, annotatedStream);
    }

    private IReadOnlyList<IValidationRule> FilterRules(IEnumerable<string>? selectedRules)
    {
        if (selectedRules is null)
            return _ruleList;

        var selectedSet = selectedRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selectedSet.Count == 0)
            return _ruleList;

        return _ruleList.Where(r => selectedSet.Contains(r.Name)).ToList();
    }
}