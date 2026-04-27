using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using V = DocumentFormat.OpenXml.Vml;
using backend.Services.Skipping;

namespace backend.Tests.Services;

public class SkipDecisionServiceTests
{
    [Fact]
    public void ShouldSkipParagraph_WhenBeforeTocBoundary_ReturnsBeforeTocReason()
    {
        using var docx = CreateDocx(new Paragraph(new Run(new Text("Before TOC"))));
        var paragraph = docx.Document.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Single();

        var decision = SkipDecisionService.ShouldSkipParagraph(
            docx.Document,
            paragraph,
            new UniversityConfig(),
            new SkipContext(ParagraphIndex: 1, FirstIncludedParagraphIndex: 2));

        Assert.True(decision.ShouldSkip);
        Assert.Equal(SkipReason.BeforeTableOfContents, decision.Reason);
    }

    [Fact]
    public void ShouldSkipParagraph_WhenTocParagraphIsInContext_ReturnsTocReason()
    {
        using var docx = CreateDocx(CreateTocFieldParagraph());
        var config = new UniversityConfig();
        config.Analysis.SkipTableOfContentsContent = true;
        var paragraph = docx.Document.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Single();
        var tocParagraphs = TableOfContentsSkipRule.GetSkippedParagraphs(docx.Document, config);

        var decision = SkipDecisionService.ShouldSkipParagraph(
            docx.Document,
            paragraph,
            config,
            new SkipContext(ParagraphIndex: 1, TableOfContentsParagraphs: tocParagraphs));

        Assert.True(decision.ShouldSkip);
        Assert.Equal(SkipReason.TableOfContents, decision.Reason);
    }

    [Fact]
    public void ShouldSkipParagraph_WhenTextBoxSkippingEnabled_ReturnsTextBoxReason()
    {
        using var docx = CreateDocx(CreateTextBoxParagraph("Text inside box"));
        var config = new UniversityConfig();
        config.Analysis.SkipTextBoxes = true;
        var paragraph = docx.Document.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Single();

        var decision = SkipDecisionService.ShouldSkipParagraph(docx.Document, paragraph, config);

        Assert.True(decision.ShouldSkip);
        Assert.Equal(SkipReason.TextBox, decision.Reason);
    }

    private static Paragraph CreateTocFieldParagraph()
    {
        return new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" TOC \\o \"1-3\" \\h \\z \\u ")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
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

    private static InMemoryDocx CreateDocx(params OpenXmlElement[] bodyChildren)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(bodyChildren));
        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

}
