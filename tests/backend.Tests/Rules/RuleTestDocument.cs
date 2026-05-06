using backend.DocumentProcessing.Content;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using A = DocumentFormat.OpenXml.Drawing;

namespace backend.Tests.Rules;

internal sealed class RuleTestDocument : IDisposable
{
    private readonly MemoryStream _stream;

    private RuleTestDocument(
        WordprocessingDocument document,
        MemoryStream stream)
    {
        Document = document;
        _stream = stream;
        Context = new RuleContext
        {
            RawDocument = Document,
            Content = new DocumentContentAnalyzer().Analyze(Document)
        };
    }

    public WordprocessingDocument Document { get; }

    public RuleContext Context { get; }

    public static RuleTestDocument Create(params OpenXmlElement[] bodyChildren)
    {
        var stream = new MemoryStream();
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var child in bodyChildren)
        {
            mainPart.Document.Body!.Append(child.CloneNode(deep: true));
        }

        mainPart.Document.Save();
        return new RuleTestDocument(document, stream);
    }

    public static Paragraph Paragraph(
        string text,
        string? styleId = null,
        ParagraphProperties? paragraphProperties = null,
        RunProperties? runProperties = null)
    {
        var paragraph = new Paragraph();
        var properties = paragraphProperties?.CloneNode(deep: true) as ParagraphProperties;

        if (!string.IsNullOrWhiteSpace(styleId))
        {
            properties ??= new ParagraphProperties();
            properties.ParagraphStyleId = new ParagraphStyleId { Val = styleId };
        }

        if (properties is not null)
        {
            paragraph.Append(properties);
        }

        var run = new Run();
        if (runProperties is not null)
        {
            run.Append(runProperties.CloneNode(deep: true));
        }

        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        paragraph.Append(run);
        return paragraph;
    }

    public static Paragraph ListParagraph(
        string text,
        int leftIndent,
        int numberingId = 1,
        int level = 0)
    {
        return Paragraph(
            text,
            paragraphProperties: new ParagraphProperties(
                new NumberingProperties(
                    new NumberingLevelReference { Val = level },
                    new NumberingId { Val = numberingId }),
                new Indentation { Left = leftIndent.ToString() }));
    }

    public static Paragraph FigureParagraph()
    {
        return new Paragraph(new Run(new Drawing(new A.Blip())));
    }

    public void Dispose()
    {
        Document.Dispose();
        _stream.Dispose();
    }
}
