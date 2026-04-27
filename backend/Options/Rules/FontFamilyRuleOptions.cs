namespace backend.RuleOptions;

public sealed class FontFamilyRuleOptions : RuleOptionsBase
{
    public const string SectionName = "Validation:Rules:FontFamily";

    public string? RequiredFontFamily { get; set; }
}
