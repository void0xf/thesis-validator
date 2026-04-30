using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class ParagraphSpacingRuleOptions : RuleOptionsBase
{
    public int[] AllowedSpacingPoints { get; set; } = [0, 6];
}
