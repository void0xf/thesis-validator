using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class ParagraphIndentRuleOptions : RuleOptionsBase
{
    public int[] AllowedIndentTwips { get; set; } = [567, 709];

    public int ToleranceTwips { get; set; } = 60;
}
