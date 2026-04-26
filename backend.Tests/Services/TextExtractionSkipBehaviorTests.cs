using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using V = DocumentFormat.OpenXml.Vml;
using backend.Services.Analysis;
using backend.Services.Extraction;

namespace backend.Tests.Services;

public class TextExtractionSkipBehaviorTests
{
    [Fact]
    public void GetParagraphText_WhenTextBoxesAreSkipped_ExcludesTextBoxText()
    {
        var paragraph = CreateTextBoxParagraph("Text inside box");
        var config = new UniversityConfig();
        config.Analysis.SkipTextBoxes = true;

        var text = TextExtractionService.GetParagraphText(paragraph, config);

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void GetParagraphText_WhenTextBoxSkippingDisabled_IncludesTextBoxText()
    {
        var paragraph = CreateTextBoxParagraph("Text inside box");
        var config = new UniversityConfig();
        config.Analysis.SkipTextBoxes = false;

        var text = TextExtractionService.GetParagraphText(paragraph, config);

        Assert.Equal("Text inside box", text);
    }

    [Fact]
    public void DescendantParagraphs_WhenSkipBeforeTocEnabled_StartsAfterDetectedToc()
    {
        using var docx = CreateDocx(
            "Before section",
            "Spis tresci",
            "Chapter body");
        var config = new UniversityConfig();
        config.Analysis.SkipBeforeTableOfContents = true;

        var paragraphs = DocumentAnalysisScope.DescendantParagraphs(docx.Document, config)
            .Select(item => TextExtractionService.GetParagraphText(item.Paragraph, config))
            .ToList();

        Assert.Equal(["Chapter body"], paragraphs);
    }

    [Fact]
    public void GetParagraphText_WhenCodeFontSkippingEnabled_ExcludesConfiguredCodeFontRuns()
    {
        using var docx = CreateDocxWithRuns(
            ("body ", "Times New Roman"),
            ("code", "Consolas"),
            (" text", "Times New Roman"));
        var paragraph = docx.Document.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Single();
        var config = new UniversityConfig();
        config.Analysis.SkipCodeFonts = true;

        var text = TextExtractionService.GetParagraphText(docx.Document, paragraph, config);

        Assert.Equal("body  text", text);
    }

    private static Paragraph CreateTextBoxParagraph(string text)
    {
        return new Paragraph(
            new Run(
                new Picture(
                    new V.Shape(
                        new V.TextBox(
                            new TextBoxContent(
                                new Paragraph(
                                    new Run(new Text(text)))))))));
    }

    private static InMemoryDocx CreateDocx(params string[] paragraphTexts)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var paragraphText in paragraphTexts)
        {
            mainPart.Document.Body!.Append(new Paragraph(new Run(new Text(paragraphText))));
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    private static InMemoryDocx CreateDocxWithRuns(params (string Text, string FontFamily)[] runs)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var paragraph = new Paragraph();

        foreach (var (text, fontFamily) in runs)
        {
            paragraph.Append(
                new Run(
                    new RunProperties(new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily }),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        }

        mainPart.Document.Body!.Append(paragraph);
        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }
}
