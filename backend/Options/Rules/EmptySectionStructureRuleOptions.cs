namespace backend.RuleOptions;

public sealed class EmptySectionStructureRuleOptions
{
    public const string SectionName = "Validation:Rules:EmptySectionStructureRule";

    public RuleAvailability Availability { get; init; } = RuleAvailability.Available;
    public RuleSeverity Severity { get; init; } = RuleSeverity.Warning;
}
