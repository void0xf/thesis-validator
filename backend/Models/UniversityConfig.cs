using DocumentFormat.OpenXml.Packaging;

namespace Backend.Models;

public class UniversityConfig
{
    public string Name { get; set; } = "Default University";
    public bool CheckGrammar { get; set; } = true;
    public FormattingConfig Formatting { get; set; } = new FormattingConfig();
}

public class FormattingConfig
{
    public FontConfig Font { get; set; } = new FontConfig();
    public LayoutConfig Layout { get; set; } = new LayoutConfig();
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
}

