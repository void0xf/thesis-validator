using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.TablesOfContents;

public enum TableOfContentsKind
{
    None,
    Automatic,
    Manual
}

public sealed record TableOfContentsDetection(
    TableOfContentsKind Kind,
    Paragraph? Paragraph,
    int ParagraphIndex,
    string? Text);
