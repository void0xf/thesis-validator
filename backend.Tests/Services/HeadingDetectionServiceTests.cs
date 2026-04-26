using backend.Tests.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using backend.Services.Structure;

namespace backend.Tests.Services;

public class HeadingDetectionServiceTests
{
    [Fact]
    public void GetHeadingLevel_ParsesEnglishStyleId()
    {
        using var docx = CreateDocxWithParagraphStyle("Heading2");
        var paragraph = GetOnlyParagraph(docx.Document);

        var level = HeadingDetectionService.GetHeadingLevel(docx.Document, paragraph);

        Assert.Equal(2, level);
    }

    [Fact]
    public void GetHeadingLevel_ParsesPolishAsciiStyleId()
    {
        using var docx = CreateDocxWithParagraphStyle("Nagwek3");
        var paragraph = GetOnlyParagraph(docx.Document);

        var level = HeadingDetectionService.GetHeadingLevel(docx.Document, paragraph);

        Assert.Equal(3, level);
    }

    [Fact]
    public void GetHeadingLevel_UsesOutlineLevelFromStyle()
    {
        using var docx = CreateDocxWithStyles(
            "CustomChapter",
            new Style(new StyleParagraphProperties(new OutlineLevel { Val = 1 }))
            {
                Type = StyleValues.Paragraph,
                StyleId = "CustomChapter"
            });
        var paragraph = GetOnlyParagraph(docx.Document);

        var level = HeadingDetectionService.GetHeadingLevel(docx.Document, paragraph);

        Assert.Equal(2, level);
    }

    [Fact]
    public void GetHeadingLevel_RejectsTocHeadingStyle()
    {
        using var docx = CreateDocxWithStyles(
            "TOCHeading",
            new Style(new StyleParagraphProperties(new OutlineLevel { Val = 0 }))
            {
                Type = StyleValues.Paragraph,
                StyleId = "TOCHeading"
            });
        var paragraph = GetOnlyParagraph(docx.Document);

        var level = HeadingDetectionService.GetHeadingLevel(docx.Document, paragraph);

        Assert.Null(level);
    }

    private static Paragraph GetOnlyParagraph(WordprocessingDocument doc)
    {
        return doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Single();
    }

    private static InMemoryDocx CreateDocxWithParagraphStyle(string styleId)
    {
        return CreateDocxWithStyles(styleId);
    }

    private static InMemoryDocx CreateDocxWithStyles(string paragraphStyleId, params Style[] styles)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(styles);
        mainPart.Document = new Document(
            new Body(
                new Paragraph(
                    new ParagraphProperties(new ParagraphStyleId { Val = paragraphStyleId }),
                    new Run(new Text("Heading text")))));
        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }
}
