using backend.Models;
using ThesisValidator.Rules;

namespace backend.ModernServices;

public sealed class ModernThesisValidatorService
{
    private readonly ModernDocumentSession _documentSession;
    private readonly DocumentContentAnalyzer _contentAnalyzer;
    private readonly ModernRuleRunner _ruleRunner;
    private readonly ModernSectionContextService _sectionContextService;
    private readonly ModernAnnotationApplier _annotationApplier;

    public ModernThesisValidatorService(
        ModernDocumentSession documentSession,
        DocumentContentAnalyzer contentAnalyzer,
        ModernRuleRunner ruleRunner,
        ModernSectionContextService sectionContextService,
        ModernAnnotationApplier annotationApplier)
    {
        _documentSession = documentSession;
        _contentAnalyzer = contentAnalyzer;
        _ruleRunner = ruleRunner;
        _sectionContextService = sectionContextService;
        _annotationApplier = annotationApplier;
    }

    public IReadOnlyList<RuleDefinition> GetAvailableRules()
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

    private IReadOnlyList<ModernRuleExecution> ValidateOpenDocument(
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
