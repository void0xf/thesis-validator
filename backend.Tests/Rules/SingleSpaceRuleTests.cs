using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;

namespace backend.Tests.Rules;

public class SingleSpaceRuleTests
{
    private readonly SingleSpaceRule _rule = new();

    private static UniversityConfig CreateConfig() => new();

    private static InMemoryDocx CreateDocxWithText(params string[] paragraphTexts)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var text in paragraphTexts)
        {
            var paragraph = new Paragraph(
                new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve })
            );
            mainPart.Document.Body!.Append(paragraph);
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    [Fact]
    public void SingleSpaces_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithText("This is a normal sentence with single spaces.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void DoubleSpaces_ReturnsError()
    {
        using var docx = CreateDocxWithText("This has  double spaces.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("2 spaces", errors[0].Message);
        Assert.Contains("SingleSpaceRule", errors[0].RuleName);
    }

    [Fact]
    public void TripleSpaces_ReturnsError()
    {
        using var docx = CreateDocxWithText("This has   triple spaces.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("3 spaces", errors[0].Message);
    }

    [Fact]
    public void MultipleViolationsInOneParagraph_ReturnsMultipleErrors()
    {
        using var docx = CreateDocxWithText("First  violation and second  violation here.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void MultipleViolationsInDifferentParagraphs_ReturnsErrors()
    {
        using var docx = CreateDocxWithText(
            "First paragraph  with error.",
            "Second paragraph  with error too."
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Equal(2, errors.Count);
        Assert.Equal(1, errors[0].Location.Paragraph);
        Assert.Equal(2, errors[1].Location.Paragraph);
    }

    [Fact]
    public void EmptyParagraph_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithText("");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void WhitespaceOnlyParagraph_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithText("   ");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ErrorMessageContainsContext()
    {
        using var docx = CreateDocxWithText("The word1  word2 in sentence.");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("word1", errors[0].Message);
        Assert.Contains("word2", errors[0].Message);
    }

    [Fact]
    public void LocationIncludesCharacterOffset()
    {
        using var docx = CreateDocxWithText("ABC  DEF");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Equal(3, errors[0].Location.CharacterOffset);
        Assert.Equal(2, errors[0].Location.Length);
    }

    [Fact]
    public void MixedContentWithSomeViolations_ReturnsOnlyErrors()
    {
        using var docx = CreateDocxWithText(
            "This is valid.",
            "This has  an error.",
            "This is also valid.",
            "Another  error here."
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Equal(2, errors.Count);
        Assert.Equal(2, errors[0].Location.Paragraph);
        Assert.Equal(4, errors[1].Location.Paragraph);
    }
}
