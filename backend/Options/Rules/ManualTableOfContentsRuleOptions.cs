namespace backend.RuleOptions;

public sealed class ManualTableOfContentsRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:ManualTableOfContents";

    public ManualTableOfContentsRuleOptions()
    {
        Severity = RuleSeverity.Warning;
    }
}
