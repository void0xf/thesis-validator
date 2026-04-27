using backend.Rules;
using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Tests.Rules;

public class ParagraphIndentRuleTests
{
    private readonly ParagraphIndentRule _rule = new();

    private static UniversityConfig CreateConfig() => new();

    private static InMemoryDocx CreateDocxWithParagraph(Paragraph paragraph)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(paragraph));
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    private static InMemoryDocx CreateDocxWithDefaultFirstLineIndent(Paragraph paragraph, int firstLineTwips)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style(
                new StyleParagraphProperties(
                    new Indentation { FirstLine = firstLineTwips.ToString() }))
            {
                Type = StyleValues.Paragraph,
                Default = true,
                StyleId = "Normal"
            });

        mainPart.Document = new Document(new Body(paragraph));
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    private static Paragraph CreateParagraph(string text, string? styleId = null)
    {
        var paragraphProperties = new ParagraphProperties();
        if (!string.IsNullOrEmpty(styleId))
        {
            paragraphProperties.ParagraphStyleId = new ParagraphStyleId { Val = styleId };
        }

        return new Paragraph(
            paragraphProperties,
            new Run(new Text(text)));
    }

    private static Paragraph CreateParagraphWithFont(string text, string fontFamily)
    {
        return new Paragraph(
            new Run(
                new RunProperties(new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily }),
                new Text(text)));
    }

    private static Paragraph CreateParagraphWithLeadingTab(string text)
    {
        return new Paragraph(
            new ParagraphProperties(),
            new Run(new TabChar()),
            new Run(new Text(text)));
    }

    private static Paragraph CreateParagraphWithFirstLineIndent(string text, int firstLineTwips)
    {
        return new Paragraph(
            new ParagraphProperties(
                new Indentation { FirstLine = firstLineTwips.ToString() }),
            new Run(new Text(text)));
    }

    private static Paragraph CreateListParagraphWithLeadingTab(string text)
    {
        return new Paragraph(
            new ParagraphProperties(
                new NumberingProperties(
                    new NumberingLevelReference { Val = 0 },
                    new NumberingId { Val = 1 })),
            new Run(new TabChar()),
            new Run(new Text(text)));
    }

    [Fact]
    public void NormalParagraphWithoutIndent_ReturnsError()
    {
        using var docx = CreateDocxWithParagraph(CreateParagraph("Normal paragraph"));

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
    }

    [Fact]
    public void ParagraphWithAllowedDirectIndent_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraph(CreateParagraphWithFirstLineIndent("Normal paragraph", 709));

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ParagraphWithAllowedInheritedIndent_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithDefaultFirstLineIndent(CreateParagraph("Normal paragraph"), 709);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ParagraphWithAllowedInheritedIndentAndLeadingTab_ReturnsError()
    {
        using var docx = CreateDocxWithDefaultFirstLineIndent(
            CreateParagraphWithLeadingTab("Normal paragraph"),
            709);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        var error = Assert.Single(errors);
        Assert.Contains("TAB character", error.Message);
    }

    [Fact]
    public void ListParagraphWithLeadingTab_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraph(CreateListParagraphWithLeadingTab("List item"));

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ExcludedStylePattern_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraph(CreateParagraph("Caption paragraph", "Caption"));

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void CodeBlockParagraph_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraph(
            CreateParagraphWithFont("public void ValidateDocument() { return; }", "Consolas"));

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }
}
