using DocumentFormat.OpenXml.Wordprocessing;

namespace ThesisValidator.Rules;

public sealed class DocumentContent
{
    public IReadOnlyList<ParagraphNode> BodyChildParagraphs { get; init; } = [];

    public IReadOnlyList<SectionNode> Sections { get; init; } = [];
}

public sealed class ParagraphNode
{
    public required Paragraph Paragraph { get; init; }

    public required int BodyIndex { get; init; }

    public required string Text { get; init; }

    public int? HeadingLevel { get; init; }

    public bool IsHeading => HeadingLevel is not null;
}

public sealed class SectionNode
{
    public required ParagraphNode Heading { get; init; }

    public bool HasIntroductoryContent { get; set; }

    public List<SectionNode> Children { get; } = [];

    public int Level => Heading.HeadingLevel ?? 0;

    public string Title => Heading.Text;
}
