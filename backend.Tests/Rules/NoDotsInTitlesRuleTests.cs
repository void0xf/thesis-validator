using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;

namespace backend.Tests.Rules;

public class NoDotsInTitlesRuleTests
{
    private readonly NoDotsInTitlesRule _rule = new();

    private static UniversityConfig CreateConfig() => new();

    private static InMemoryDocx CreateDocxWithStyledParagraph(string styleId, string text)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var paragraph = new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = styleId }
            ),
            new Run(new Text(text))
        );
        mainPart.Document.Body!.Append(paragraph);

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    private static InMemoryDocx CreateDocxWithNormalParagraph(string text)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var paragraph = new Paragraph(
            new Run(new Text(text))
        );
        mainPart.Document.Body!.Append(paragraph);

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    [Theory]
    [InlineData("Heading1", "Introduction.")]
    [InlineData("Heading2", "Chapter One.")]
    [InlineData("Nagwek1", "Rozdział pierwszy.")]
    [InlineData("Title", "My Thesis Title.")]
    [InlineData("Tytu", "Tytuł pracy.")]
    [InlineData("Subtitle", "A Subtitle.")]
    [InlineData("Podtytu", "Podtytuł.")]
    [InlineData("Caption", "Figure 1.")]
    [InlineData("Podpis", "Rysunek 1.")]
    public void TitleStyleEndingWithPeriod_ReturnsError(string styleId, string text)
    {
        using var docx = CreateDocxWithStyledParagraph(styleId, text);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("should not end with a period", errors[0].Message);
        Assert.Contains(styleId, errors[0].Message);
    }

    [Theory]
    [InlineData("Heading1", "Introduction")]
    [InlineData("Heading2", "Chapter One")]
    [InlineData("Title", "My Thesis Title")]
    [InlineData("Caption", "Figure 1: Description")]
    public void TitleStyleNotEndingWithPeriod_ReturnsNoErrors(string styleId, string text)
    {
        using var docx = CreateDocxWithStyledParagraph(styleId, text);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Heading1", "To be continued...")]
    [InlineData("Title", "The Story Goes On...")]
    [InlineData("Caption", "And more...")]
    public void TitleStyleEndingWithEllipsis_ReturnsNoErrors(string styleId, string text)
    {
        using var docx = CreateDocxWithStyledParagraph(styleId, text);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Heading1", "Why This Matters?")]
    [InlineData("Title", "What Is the Answer?")]
    [InlineData("Caption", "Is This Correct?")]
    public void TitleStyleEndingWithQuestionMark_ReturnsNoErrors(string styleId, string text)
    {
        using var docx = CreateDocxWithStyledParagraph(styleId, text);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Heading1", "Important Notice!")]
    [InlineData("Title", "Breaking News!")]
    public void TitleStyleEndingWithExclamation_ReturnsNoErrors(string styleId, string text)
    {
        using var docx = CreateDocxWithStyledParagraph(styleId, text);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void NormalParagraphEndingWithPeriod_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithNormalParagraph("This is a normal sentence.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void NonTargetStyleEndingWithPeriod_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithStyledParagraph("Normal", "This is normal text.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void EmptyHeading_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithStyledParagraph("Heading1", "");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void HeadingWithTrailingSpacesAndPeriod_ReturnsError()
    {
        using var docx = CreateDocxWithStyledParagraph("Heading1", "Introduction.   ");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
    }

    [Fact]
    public void HeadingWithOnlyPeriod_ReturnsError()
    {
        using var docx = CreateDocxWithStyledParagraph("Heading1", ".");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
    }

    [Fact]
    public void HeadingEndingWithColon_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithStyledParagraph("Heading1", "Chapter 1:");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void HeadingWithPeriodInMiddle_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithStyledParagraph("Heading1", "Dr. Smith's Research");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void MultipleHeadingsWithMixedEndings_ReturnsCorrectErrors()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
            new Run(new Text("Chapter One"))
        ));

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading2" }),
            new Run(new Text("Section 1.1."))
        ));

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Title" }),
            new Run(new Text("My Thesis"))
        ));

        mainPart.Document.Body!.Append(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Caption" }),
            new Run(new Text("Figure 1."))
        ));

        mainPart.Document.Save();
        using var docx = new InMemoryDocx(doc, stream);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Equal(2, errors.Count);
        Assert.Contains("Heading2", errors[0].Message);
        Assert.Contains("Caption", errors[1].Message);
    }
}
