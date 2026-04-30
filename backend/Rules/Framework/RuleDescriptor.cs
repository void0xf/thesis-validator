
namespace ThesisValidator.Rules;

public sealed record RuleDescriptor(
    string Name,
    string DisplayName,
    string Description,
    string Category,
    RuleAvailability DefaultAvailability,
    RuleSeverity DefaultSeverity);