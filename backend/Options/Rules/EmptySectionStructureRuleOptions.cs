namespace backend.RuleOptions;

public sealed class EmptySectionStructureRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:EmptySectionStructureRule";

    public EmptySectionStructureRuleOptions()
    {
        Severity = RuleSeverity.Warning;
    }
}
