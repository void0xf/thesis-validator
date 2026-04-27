namespace backend.RuleOptions;

public sealed class HeadingStyleUsageRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:HeadingStyleUsageRule";

    public int FontSizeThresholdAboveBodyPt { get; set; } = 2;
    public int MaxHeadingTextLength { get; set; } = 200;
}
