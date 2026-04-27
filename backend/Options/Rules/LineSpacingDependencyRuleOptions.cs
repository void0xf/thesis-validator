namespace backend.RuleOptions;

public sealed class LineSpacingDependencyRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:LineSpacingDependencyRule";

    public int TargetLineSpacingTwips { get; set; } = 360;
}
