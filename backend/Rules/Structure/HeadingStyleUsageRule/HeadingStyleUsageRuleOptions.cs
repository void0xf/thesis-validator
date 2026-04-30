using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class HeadingStyleUsageRuleOptions : RuleOptionsBase
{
    public int FontSizeThresholdAboveBodyPt { get; set; } = 2;

    public int MaxHeadingTextLength { get; set; } = 200;
}
