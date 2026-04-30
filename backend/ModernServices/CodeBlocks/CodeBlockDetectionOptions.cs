namespace backend.ModernServices.CodeBlocks;

public sealed class CodeBlockDetectionOptions
{
    public const string SectionName = "Validation:CodeBlockDetection";

    public double MinimumCodeFontTextRatio { get; set; } = 0.7;

    public bool RequireWholeParagraphMonospace { get; set; }

    public List<string> CodeFonts { get; set; } =
    [
        "Consolas",
        "Courier New",
        "Courier",
        "Lucida Console",
        "Cascadia Code",
        "Cascadia Mono",
        "JetBrains Mono",
        "Fira Code",
        "Source Code Pro",
        "Menlo",
        "Monaco",
        "DejaVu Sans Mono",
        "Liberation Mono"
    ];
}
