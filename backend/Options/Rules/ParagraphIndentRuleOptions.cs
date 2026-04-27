namespace backend.RuleOptions;

public sealed class ParagraphIndentRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:RequiredIndentCm";

    public int[] AllowedIndentTwips { get; set; } = [567, 709];

    public int ToleranceTwips { get; set; } = 60;
}
