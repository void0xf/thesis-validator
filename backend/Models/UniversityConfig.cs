using DocumentFormat.OpenXml.Packaging;

namespace Backend.Models;

public class UniversityConfig
{
    public string Name { get; set; } = "Default University";
    public bool CheckGrammar { get; set; } = true;
    public string Language { get; set; } = "pl-PL";
    public FormattingConfig Formatting { get; set; } = new FormattingConfig();
    public AnalysisConfig Analysis { get; set; } = new AnalysisConfig();
    public RuleSettingsConfig Rules { get; set; } = new RuleSettingsConfig();
}

public class FormattingConfig
{
    public FontConfig Font { get; set; } = new FontConfig();
    public LayoutConfig Layout { get; set; } = new LayoutConfig();
    public bool CheckTableOfContents { get; set; } = true;
    public bool SkipBeforeTableOfContents { get; set; }
    public bool SkipTextBoxes { get; set; } = true;
    public bool SkipTableOfContentsContent { get; set; } = true;
}

public class AnalysisConfig
{
    public bool? SkipBeforeTableOfContents { get; set; }
    public bool? SkipTextBoxes { get; set; }
    public bool? SkipTableOfContentsContent { get; set; }
    public bool SkipCodeFonts { get; set; }
    public List<string> CodeFontFamilies { get; set; } =
    [
        "Consolas",
        "Courier New",
        "Courier",
        "Menlo",
        "Monaco",
        "Monospace"
    ];
}

public class RuleSettingsConfig
{
    public Dictionary<string, RuleOverrideConfig> Overrides { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public RuleOverrideConfig? GetOverride(string ruleId)
    {
        if (Overrides.TryGetValue(ruleId, out var ruleOverride))
            return ruleOverride;

        var match = Overrides.FirstOrDefault(pair =>
            string.Equals(pair.Key, ruleId, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrEmpty(match.Key) ? null : match.Value;
    }
}

public class RuleOverrideConfig
{
    public string? Severity { get; set; }
    public bool? Enabled { get; set; }
}

public class FontConfig
{
    public string FontFamily { get; set; } = "Times New Roman";
    public int FontSize { get; set; } = 12;
}

public class LayoutConfig
{
    public double MarginLeft { get; set; } = 2.5;
    public double MarginRight { get; set; } = 2.5;
    public double RequiredIndentCm { get; set; } = 1.25;
    public List<int> ParagraphSpacingRule { get; set; } = new List<int>() { 0, 6 };
}
