namespace backend.ModernServices.CodeBlocks;

public sealed class CodeBlockDetectionResult
{
    public bool IsCodeBlock { get; init; }

    public double CodeFontTextRatio { get; init; }

    public IReadOnlyList<string> DetectedFonts { get; init; } = Array.Empty<string>();

    public string? MatchedFont { get; init; }

    public int TotalTextLength { get; init; }

    public int CodeFontTextLength { get; init; }
}
