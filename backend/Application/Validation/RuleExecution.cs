using backend.Models;
using ThesisValidator.Rules;

namespace backend.Application.Validation;

public sealed record RuleExecution(
    ValidationResult Result,
    RuleProblem Problem);
