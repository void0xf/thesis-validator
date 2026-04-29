using backend.RuleOptions;

namespace ThesisValidator.Rules;

public sealed record RulePolicy(
    RuleAvailability Availability,
    RuleSeverity Severity);
