using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;

namespace backend.Tests.Rules;

public class TextJustificationRuleTests
{
    private readonly TextJustificationRule _rule = new();

    private static UniversityConfig CreateConfig() => new();

    private static InMemoryDocx CreateDocxWithJustification(JustificationValues? justification, string text = "Sample text content.")
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var paragraphProps = new ParagraphProperties();
        if (justification.HasValue)
            paragraphProps.Justification = new Justification { Val = justification.Value };

        var paragraph = new Paragraph(paragraphProps, new Run(new Text(text)));
        mainPart.Document.Body!.Append(paragraph);
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    private static InMemoryDocx CreateDocxWithStyle(string styleId, string text = "Sample text content.")
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var paragraph = new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
            new Run(new Text(text))
        );
        mainPart.Document.Body!.Append(paragraph);
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    private static InMemoryDocx CreateDocxWithListItem(string text = "List item text.")
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var paragraph = new Paragraph(
            new ParagraphProperties(
                new NumberingProperties(
                    new NumberingLevelReference { Val = 0 },
                    new NumberingId { Val = 1 }
                )
            ),
            new Run(new Text(text))
        );
        mainPart.Document.Body!.Append(paragraph);
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    [Fact]
    public void FullJustification_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithJustification(JustificationValues.Both);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void LeftAligned_ReturnsError()
    {
        using var docx = CreateDocxWithJustification(JustificationValues.Left);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("left aligned", errors[0].Message);
        Assert.Contains("full justification", errors[0].Message);
    }

    [Fact]
    public void CenterAligned_ReturnsError()
    {
        using var docx = CreateDocxWithJustification(JustificationValues.Center);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("center aligned", errors[0].Message);
    }

    [Fact]
    public void RightAligned_ReturnsError()
    {
        using var docx = CreateDocxWithJustification(JustificationValues.Right);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("right aligned", errors[0].Message);
    }

    [Fact]
    public void NoJustificationSet_DefaultsToLeftAndReturnsError()
    {
        using var docx = CreateDocxWithJustification(null);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("left aligned", errors[0].Message);
    }

    [Fact]
    public void EmptyParagraph_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithJustification(JustificationValues.Left, "");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ListItem_SkippedEvenIfNotJustified()
    {
        using var docx = CreateDocxWithListItem();

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Heading1")]
    [InlineData("Heading2")]
    [InlineData("Nagwek1")]
    [InlineData("Title")]
    [InlineData("Tytu")]
    [InlineData("Subtitle")]
    [InlineData("Podtytu")]
    [InlineData("TOC1")]
    [InlineData("TOCHeading")]
    [InlineData("Spistreci1")]
    [InlineData("Caption")]
    [InlineData("Quote")]
    [InlineData("Cytat")]
    public void ExcludedStyles_SkippedEvenIfNotJustified(string styleId)
    {
        using var docx = CreateDocxWithStyle(styleId);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void NormalStyleLeftAligned_ReturnsError()
    {
        using var docx = CreateDocxWithStyle("Normal");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
    }

    [Fact]
    public void ErrorMessageContainsTextPreview()
    {
        using var docx = CreateDocxWithJustification(JustificationValues.Left, "This is the paragraph text that should appear in the error.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("This is the paragraph", errors[0].Location.Text);
    }

    [Fact]
    public void MultipleParagraphsMixedAlignment_ReturnsErrorsOnlyForNonJustified()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Both }),
            new Run(new Text("Justified text"))
        ));

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
            new Run(new Text("Left aligned text"))
        ));

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Both }),
            new Run(new Text("Another justified text"))
        ));

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(new Text("Center aligned text"))
        ));

        mainPart.Document.Save();
        using var docx = new InMemoryDocx(doc, stream);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Equal(2, errors.Count);
        Assert.Contains("left aligned", errors[0].Message);
        Assert.Contains("center aligned", errors[1].Message);
    }
}
