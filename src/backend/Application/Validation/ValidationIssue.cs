using ThesisValidator.Rules;

namespace backend.Application.Validation;

public sealed class ValidationIssue
{
    public string RuleName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public RuleSeverity Severity { get; set; } = RuleSeverity.Error;

    public bool IsError => Severity == RuleSeverity.Error;

    public string? Category { get; set; }

    public ParagraphIndexKind ParagraphIndexKind { get; set; } = ParagraphIndexKind.Descendant;

    public DocumentLocation Location { get; set; } = new();
}
