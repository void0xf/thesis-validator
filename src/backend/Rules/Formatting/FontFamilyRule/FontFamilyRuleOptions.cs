using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class FontFamilyRuleOptions : RuleOptionsBase
{
    public string? RequiredFontFamily { get; set; }
}
