using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class HierarchyDepthRuleOptions : RuleOptionsBase
{
    public int MaxAllowedLevel { get; set; } = 3;
}
