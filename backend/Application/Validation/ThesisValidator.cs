using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Content;
using backend.Annotation;
using backend.Models;
using ThesisValidator.Rules;

namespace backend.Application.Validation;

public sealed class ThesisValidator
{
    private readonly DocumentSession _documentSession;
    private readonly DocumentContentAnalyzer _contentAnalyzer;
    private readonly RuleRunner _ruleRunner;
    private readonly SectionContextResolver _sectionContextService;
    private readonly AnnotationApplicator _annotationApplier;

    public ThesisValidator(
        DocumentSession documentSession,
        DocumentContentAnalyzer contentAnalyzer,
        RuleRunner ruleRunner,
        SectionContextResolver sectionContextService,
        AnnotationApplicator annotationApplier)
    {
        _documentSession = documentSession;
        _contentAnalyzer = contentAnalyzer;
        _ruleRunner = ruleRunner;
        _sectionContextService = sectionContextService;
        _annotationApplier = annotationApplier;
    }

    public IReadOnlyList<AvailableValidationRule> GetAvailableRules()
    {
        return _ruleRunner.GetAvailableRules();
    }

    public IReadOnlyList<string> GetUnknownRuleNames(IEnumerable<string> selectedRules)
    {
        return _ruleRunner.GetUnknownRuleNames(selectedRules);
    }

    public IReadOnlyList<ValidationResult> Validate(
        Stream fileStream,
        IEnumerable<string>? selectedRules = null)
    {
        using var document = _documentSession.OpenRead(fileStream);
        return ValidateOpenDocument(document, selectedRules)
            .Select(execution => execution.Result)
            .ToList();
    }

    public (IReadOnlyList<ValidationResult> Results, MemoryStream AnnotatedDocument) ValidateWithComments(
        Stream fileStream,
        IEnumerable<string>? selectedRules = null)
    {
        using var editableDocument = _documentSession.OpenEditableCopy(fileStream);
        var executions = ValidateOpenDocument(editableDocument.Document, selectedRules);

        _annotationApplier.Apply(editableDocument.Document, executions);

        return (
            executions.Select(execution => execution.Result).ToList(),
            _documentSession.SaveAnnotated(editableDocument.Document));
    }

    private IReadOnlyList<RuleExecution> ValidateOpenDocument(
        DocumentFormat.OpenXml.Packaging.WordprocessingDocument document,
        IEnumerable<string>? selectedRules)
    {
        var content = _contentAnalyzer.Analyze(document);
        var context = new RuleContext
        {
            RawDocument = document,
            Content = content
        };

        var executions = _ruleRunner.Run(context, selectedRules);
        var results = executions
            .Select(execution => execution.Result)
            .ToList();

        _sectionContextService.PopulateSectionContext(results, content);
        return executions;
    }
}
