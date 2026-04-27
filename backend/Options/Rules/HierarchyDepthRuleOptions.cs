namespace backend.RuleOptions;

public sealed class HierarchyDepthRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:HierarchyDepthRule";

    public int MaxAllowedLevel { get; set; } = 3;
}
