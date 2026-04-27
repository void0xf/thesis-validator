namespace backend.RuleOptions;

public abstract class RuleOptionsBase
{
    public RuleAvailability Availability { get; set; } = RuleAvailability.Available;
    public RuleSeverity Severity { get; set; } = RuleSeverity.Error;
}
