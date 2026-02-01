using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;

namespace backend.Tests.Rules;

public class ListConsistencyRuleTests
{
    private readonly ListConsistencyRule _rule = new();

    private static UniversityConfig CreateConfig() => new();

    private static Paragraph CreateListItem(string text, int numberingId, int level = 0, int? indentTwips = null)
    {
        var numberingProps = new NumberingProperties(
            new NumberingLevelReference { Val = level },
            new NumberingId { Val = numberingId }
        );

        var paraProps = new ParagraphProperties(numberingProps);

        if (indentTwips.HasValue)
        {
            paraProps.Indentation = new Indentation { Left = indentTwips.Value.ToString() };
        }

        return new Paragraph(
            paraProps,
            new Run(new Text(text))
        );
    }

    private static InMemoryDocx CreateDocxWithParagraphs(params Paragraph[] paragraphs)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var para in paragraphs)
        {
            mainPart.Document.Body!.Append(para);
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    [Fact]
    public void ConsistentPunctuationWithSemicolons_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item;", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ConsistentPunctuationWithCommas_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item,", 1),
            CreateListItem("Second item,", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void MixedPunctuation_ReturnsErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item,", 1),
            CreateListItem("Third item;", 1),
            CreateListItem("Fourth item.", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("','", errors[0].Message);
        Assert.Contains("';'", errors[0].Message);
    }

    [Fact]
    public void LastItemNotEndingWithPeriod_ReturnsError()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item;", 1),
            CreateListItem("Third item;", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Last list item should end with period", errors[0].Message);
    }

    [Fact]
    public void MiddleItemMissingPunctuation_ReturnsError()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("no punctuation", errors[0].Message);
        Assert.Contains("';'", errors[0].Message);
    }

    [Fact]
    public void SingleItemList_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("Only item.", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void FirstItemNoPunctuation_MiddleItemsMustMatch()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item", 1),
            CreateListItem("Second item;", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("';'", errors[0].Message);
        Assert.Contains("no punctuation", errors[0].Message);
    }

    [Fact]
    public void AllItemsNoPunctuation_LastMustEndWithPeriod()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item", 1),
            CreateListItem("Second item", 1),
            CreateListItem("Third item", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Last list item should end with period", errors[0].Message);
    }

    [Fact]
    public void ConsistentIndentation_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Second item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Third item.", 1, level: 0, indentTwips: 720)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void InconsistentIndentation_ReturnsError()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Second item;", 1, level: 0, indentTwips: 1440),
            CreateListItem("Third item.", 1, level: 0, indentTwips: 720)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("inconsistent indentation", errors[0].Message);
    }

    [Fact]
    public void DifferentLevelsWithDifferentIndentation_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Sub item;", 1, level: 1, indentTwips: 1440),
            CreateListItem("Second item.", 1, level: 0, indentTwips: 720)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        var indentErrors = errors.Where(e => e.Message.Contains("indentation")).ToList();
        Assert.Empty(indentErrors);
    }

    [Fact]
    public void TwoSeparateLists_CheckedIndependently()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("List1 item1;", 1),
            CreateListItem("List1 item2.", 1),
            new Paragraph(new Run(new Text("Normal paragraph between lists"))),
            CreateListItem("List2 item1,", 2),
            CreateListItem("List2 item2.", 2)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void TwoListsBothWithErrors_ReportsAllErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("List1 item1;", 1),
            CreateListItem("List1 item2;", 1),
            new Paragraph(new Run(new Text("Normal paragraph"))),
            CreateListItem("List2 item1,", 2),
            CreateListItem("List2 item2,", 2)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void EmptyListItem_HandledGracefully()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("no punctuation", errors[0].Message);
        Assert.Contains("';'", errors[0].Message);
    }

    [Fact]
    public void NoLists_ReturnsNoErrors()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            new Paragraph(new Run(new Text("Normal paragraph 1."))),
            new Paragraph(new Run(new Text("Normal paragraph 2.")))
        ));
        mainPart.Document.Save();
        using var docx = new InMemoryDocx(doc, stream);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void NestedListLevels_CheckedSeparately()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("Main item 1:", 1, level: 0),
            CreateListItem("Sub item a;", 1, level: 1),
            CreateListItem("Sub item b.", 1, level: 1),
            CreateListItem("Main item 2.", 1, level: 0)
        );

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.NotNull(errors);
    }
}
