using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class NoDotsInTitlesRuleOptions : RuleOptionsBase
{
    public string[] TargetStylePatterns { get; set; } =
    [
        "heading",
        "nagwek",
        "title",
        "tytu",
        "subtitle",
        "podtytu",
        "caption",
        "podpis"
    ];
}
