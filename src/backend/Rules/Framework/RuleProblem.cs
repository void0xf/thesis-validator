namespace ThesisValidator.Rules;

public sealed record RuleProblem(
    string Message,
    DocumentLocation Location,
    ParagraphIndexKind ParagraphIndexKind = ParagraphIndexKind.Descendant,
    AnnotationTarget? AnnotationTarget = null);
