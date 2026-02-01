using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Tests.Helpers;

public static class DocxTestHelper
{
    public static WordprocessingDocument OpenDocxAsRead(string relativePath)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath);
        return WordprocessingDocument.Open(filePath, false);
    }

    /// <summary>
    /// Creates an in-memory WordprocessingDocument with the specified paragraphs.
    /// Each tuple contains (text, fontFamily). If fontFamily is null, no explicit font is set.
    /// The returned InMemoryDocx should be disposed which will dispose both the document and stream.
    /// </summary>
    public static InMemoryDocx CreateInMemoryDocx(
        params (string Text, string? FontFamily)[] paragraphs)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var (text, fontFamily) in paragraphs)
        {
            var run = new Run(new Text(text));

            if (!string.IsNullOrEmpty(fontFamily))
            {
                run.RunProperties = new RunProperties(
                    new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily }
                );
            }

            var paragraph = new Paragraph(run);
            mainPart.Document.Body!.Append(paragraph);
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    /// <summary>
    /// Creates an in-memory WordprocessingDocument with a default font style.
    /// </summary>
    public static InMemoryDocx CreateInMemoryDocxWithDefaultFont(
        string defaultFont,
        params string[] paragraphTexts)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        // Add styles part with default font
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style(
                new StyleRunProperties(
                    new RunFonts { Ascii = defaultFont, HighAnsi = defaultFont }
                )
            )
            {
                Type = StyleValues.Paragraph,
                Default = true,
                StyleId = "Normal"
            }
        );

        foreach (var text in paragraphTexts)
        {
            var run = new Run(new Text(text));
            var paragraph = new Paragraph(run);
            mainPart.Document.Body!.Append(paragraph);
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    /// <summary>
    /// Creates an in-memory WordprocessingDocument with multiple runs in a single paragraph.
    /// Each tuple contains (text, fontFamily). Useful for testing character offset tracking.
    /// </summary>
    public static InMemoryDocx CreateInMemoryDocxWithMultipleRuns(
        params (string Text, string? FontFamily)[] runs)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var paragraph = new Paragraph();
        foreach (var (text, fontFamily) in runs)
        {
            var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            if (!string.IsNullOrEmpty(fontFamily))
            {
                run.RunProperties = new RunProperties(
                    new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily }
                );
            }

            paragraph.Append(run);
        }

        mainPart.Document.Body!.Append(paragraph);
        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }
}

/// <summary>
/// Wrapper that ensures document is disposed before stream.
/// </summary>
public sealed class InMemoryDocx : IDisposable
{
    public WordprocessingDocument Document { get; }
    private readonly MemoryStream _stream;

    public InMemoryDocx(WordprocessingDocument doc, MemoryStream stream)
    {
        Document = doc;
        _stream = stream;
    }

    public void Dispose()
    {
        Document.Dispose();
        _stream.Dispose();
    }
}