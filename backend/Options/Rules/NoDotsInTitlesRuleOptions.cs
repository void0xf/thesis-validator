namespace backend.RuleOptions;

public sealed class NoDotsInTitlesRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:NoDotsInTitlesRule";

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
