using backend.Tests.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using backend.Services.Formatting;

namespace backend.Tests.Services;

public class FormattingResolutionServiceTests
{
    [Fact]
    public void ResolveFormatting_UsesDirectParagraphProperties()
    {
        using var docx = CreateDocxWithParagraph(
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "120" }),
                new Run(new Text("Body"))));
        var paragraph = GetOnlyParagraph(docx.Document);

        Assert.Equal(JustificationValues.Center, FormattingResolutionService.ResolveJustification(docx.Document, paragraph));
        Assert.Equal(120, FormattingResolutionService.ResolveSpacingAfter(docx.Document, paragraph));
    }

    [Fact]
    public void ResolveFormatting_WalksBasedOnStyleChain()
    {
        using var docx = CreateDocxWithStyles(
            new Styles(
                new Style(
                    new StyleParagraphProperties(
                        new Justification { Val = JustificationValues.Both },
                        new SpacingBetweenLines { After = "120" },
                        new Indentation { FirstLine = "567" }),
                    new StyleRunProperties(new FontSize { Val = "28" }))
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "BaseStyle"
                },
                new Style(new BasedOn { Val = "BaseStyle" })
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "ChildStyle"
                }),
            "ChildStyle");
        var paragraph = GetOnlyParagraph(docx.Document);
        var run = paragraph.Elements<Run>().Single();

        Assert.Equal(JustificationValues.Both, FormattingResolutionService.ResolveJustification(docx.Document, paragraph));
        Assert.Equal(120, FormattingResolutionService.ResolveSpacingAfter(docx.Document, paragraph));
        Assert.Equal(567, FormattingResolutionService.ResolveFirstLineIndent(docx.Document, paragraph));
        Assert.Equal(14.0, FormattingResolutionService.ResolveFontSizePt(docx.Document, paragraph, run));
    }

    [Fact]
    public void ResolveFormatting_UsesDefaultParagraphStyle()
    {
        using var docx = CreateDocxWithStyles(
            new Styles(
                new Style(
                    new StyleParagraphProperties(new SpacingBetweenLines { After = "240" }))
                {
                    Type = StyleValues.Paragraph,
                    Default = true,
                    StyleId = "Normal"
                }),
            styleId: null);
        var paragraph = GetOnlyParagraph(docx.Document);

        Assert.Equal(240, FormattingResolutionService.ResolveSpacingAfter(docx.Document, paragraph));
    }

    [Fact]
    public void ResolveFormatting_UsesDocumentDefaults()
    {
        using var docx = CreateDocxWithStyles(
            new Styles(
                new DocDefaults(
                    new RunPropertiesDefault(
                        new RunPropertiesBaseStyle(new FontSize { Val = "24" })),
                    new ParagraphPropertiesDefault(
                        new ParagraphPropertiesBaseStyle(
                            new Justification { Val = JustificationValues.Both },
                            new SpacingBetweenLines { After = "80" })))),
            styleId: null);
        var paragraph = GetOnlyParagraph(docx.Document);
        var run = paragraph.Elements<Run>().Single();

        Assert.Equal(12.0, FormattingResolutionService.ResolveFontSizePt(docx.Document, paragraph, run));
        Assert.Equal(JustificationValues.Both, FormattingResolutionService.ResolveJustification(docx.Document, paragraph));
        Assert.Equal(80, FormattingResolutionService.ResolveSpacingAfter(docx.Document, paragraph));
    }

    private static Paragraph GetOnlyParagraph(WordprocessingDocument doc)
    {
        return doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Single();
    }

    private static InMemoryDocx CreateDocxWithParagraph(Paragraph paragraph)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(paragraph));
        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    private static InMemoryDocx CreateDocxWithStyles(Styles styles, string? styleId)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = styles;

        var paragraphProperties = string.IsNullOrEmpty(styleId)
            ? new ParagraphProperties()
            : new ParagraphProperties(new ParagraphStyleId { Val = styleId });

        mainPart.Document = new Document(
            new Body(
                new Paragraph(
                    paragraphProperties,
                    new Run(new Text("Body")))));
        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }
}
