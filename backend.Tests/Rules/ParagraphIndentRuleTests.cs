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

    [Fact]
    public void NormalParagraphWithoutIndent_ReturnsError()
    {
        using var docx = CreateDocxWithParagraph(CreateParagraph("Normal paragraph"));

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
    }

    [Fact]
    public void ExcludedStylePattern_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraph(CreateParagraph("Caption paragraph", "Caption"));

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }
}
