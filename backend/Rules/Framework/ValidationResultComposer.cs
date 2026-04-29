using backend.Models;

namespace ThesisValidator.Rules;

public sealed class ValidationResultComposer
{
    public ValidationResult Compose(
        RuleDescriptor descriptor,
        RulePolicy policy,
        RuleProblem problem)
    {
        return new ValidationResult
        {
            RuleName = descriptor.Name,
            Message = problem.Message,
            Category = descriptor.Category,
            Severity = policy.Severity.ToString(),
            ParagraphIndexKind = problem.ParagraphIndexKind,
            Location = problem.Location
        };
    }
}
