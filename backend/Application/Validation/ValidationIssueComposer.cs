using ThesisValidator.Rules;

namespace backend.Application.Validation;

public sealed class ValidationIssueComposer
{
    public ValidationIssue Compose(
        RuleDescriptor descriptor,
        RulePolicy policy,
        RuleProblem problem)
    {
        return new ValidationIssue
        {
            RuleName = descriptor.Name,
            Message = problem.Message,
            Category = descriptor.Category,
            Severity = policy.Severity,
            ParagraphIndexKind = problem.ParagraphIndexKind,
            Location = problem.Location
        };
    }
}
