using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.Lists;

public sealed record ListItem(
    Paragraph Paragraph,
    int ParagraphIndex,
    int Level,
    int IndentLeft,
    string Text);
