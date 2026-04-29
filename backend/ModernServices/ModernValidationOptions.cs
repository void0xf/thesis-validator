namespace backend.ModernServices;

public sealed class ModernValidationOptions
{
    public const string SectionName = "Validation:Skipping";

    public bool SkipBeforeTableOfContents { get; init; }

    public bool SkipTextBoxes { get; init; } = true;

    public bool SkipTableOfContentsContent { get; init; } = true;
}
