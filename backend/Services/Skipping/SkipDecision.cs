using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Skipping;

public enum SkipReason
{
    None,
    TextBox,
    TableOfContents,
    BeforeTableOfContents,
    CodeFont,
    StructuralStyle
}

public sealed record SkipDecision(
    bool ShouldSkip,
    SkipReason Reason = SkipReason.None,
    string? Detail = null)
{
    public static SkipDecision Include { get; } = new(false);

    public static SkipDecision Skip(SkipReason reason, string? detail = null)
    {
        return new SkipDecision(true, reason, detail);
    }
}

public sealed record SkipContext(
    int? ParagraphIndex = null,
    int? FirstIncludedParagraphIndex = null,
    IReadOnlySet<Paragraph>? TableOfContentsParagraphs = null);
