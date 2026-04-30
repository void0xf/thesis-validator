using ThesisValidator.Rules;

namespace backend.Application.Validation;

public sealed record RuleExecution(
    ValidationIssue Result,
    RuleProblem Problem);
