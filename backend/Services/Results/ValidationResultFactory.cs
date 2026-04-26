using backend.Models;
using Backend.Models;
using ThesisValidator.Rules;

namespace backend.Services.Results;

public static class ValidationResultFactory
{
    public static ValidationResult Create(
        string ruleId,
        UniversityConfig config,
        string message,
        DocumentLocation? location = null,
        ParagraphIndexKind indexKind = ParagraphIndexKind.Descendant,
        string? severity = null,
        string? category = null)
    {
        var definition = RuleCatalog.GetDefinition(ruleId);
        return new ValidationResult
        {
            RuleName = ruleId,
            Message = message,
            Severity = SeverityResolver.Resolve(ruleId, config, severity),
            Category = category ?? definition.Category,
            ParagraphIndexKind = indexKind,
            Location = location ?? new DocumentLocation()
        };
    }

    public static ValidationResult ForParagraph(
        string ruleId,
        UniversityConfig config,
        string message,
        int paragraph,
        string text,
        ParagraphIndexKind indexKind = ParagraphIndexKind.Descendant,
        string? severity = null,
        string? category = null)
    {
        return Create(
            ruleId,
            config,
            message,
            new DocumentLocation
            {
                Paragraph = paragraph,
                Text = text
            },
            indexKind,
            severity,
            category);
    }

    public static ValidationResult ForRun(
        string ruleId,
        UniversityConfig config,
        string message,
        int paragraph,
        int run,
        int characterOffset,
        int length,
        string text,
        ParagraphIndexKind indexKind = ParagraphIndexKind.Descendant,
        string? severity = null,
        string? category = null)
    {
        return Create(
            ruleId,
            config,
            message,
            new DocumentLocation
            {
                Paragraph = paragraph,
                Run = run,
                CharacterOffset = characterOffset,
                Length = length,
                Text = text
            },
            indexKind,
            severity,
            category);
    }
}
