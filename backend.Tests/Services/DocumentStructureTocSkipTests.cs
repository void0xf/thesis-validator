using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using backend.Services.Analysis;

namespace backend.Tests.Services;

public class DocumentStructureTocSkipTests
{
    [Fact]
    public void ExtractHeadings_WhenSkipBeforeTocDisabled_ReturnsTocHeading()
    {
        using var docx = CreateDocxWithParagraphs(
            ("Title page", "Normal"),
            ("Spis tresci", "TOCHeading"),
            ("Chapter 1", "Heading1"));

        var headings = ThesisValidatorService.ExtractHeadings(docx.Document, new UniversityConfig());

        Assert.Collection(
            headings,
            heading => Assert.Equal("Spis tresci", heading.Text),
            heading => Assert.Equal("Chapter 1", heading.Text));
    }

    [Fact]
    public void ExtractHeadings_WhenSkipBeforeTocEnabled_ReturnsTocHeading()
    {
        using var docx = CreateDocxWithParagraphs(
            ("Title page", "Heading1"),
            ("Spis tresci", "TOCHeading"),
            ("Chapter 1", "Heading1"));

        var config = new UniversityConfig();
        config.Formatting.SkipBeforeTableOfContents = true;

        var headings = ThesisValidatorService.ExtractHeadings(docx.Document, config);

        Assert.Collection(
            headings,
            heading => Assert.Equal("Spis tresci", heading.Text),
            heading => Assert.Equal("Chapter 1", heading.Text));
    }

    private static InMemoryDocx CreateDocxWithParagraphs(params (string Text, string StyleId)[] paragraphs)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var (text, styleId) in paragraphs)
        {
            mainPart.Document.Body!.Append(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
                new Run(new Text(text))));
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }
}
