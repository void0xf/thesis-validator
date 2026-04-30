using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class LineSpacingDependencyRuleOptions : RuleOptionsBase
{
    public int TargetLineSpacingTwips { get; set; } = 360;
}
