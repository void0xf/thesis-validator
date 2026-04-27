using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;

namespace backend.Tests.Rules;

public class ListSplitRuleTests
{
    private readonly ListPunctuationConsistencyRule _punctuationRule = new();
    private readonly ListIndentationConsistencyRule _indentationRule = new();

    private static UniversityConfig CreateConfig() => new();

    private static Paragraph CreateListItem(
        string text,
        int numberingId,
        int level = 0,
        int? indentTwips = null,
        string? styleId = null)
    {
        var numberingProps = new NumberingProperties(
            new NumberingLevelReference { Val = level },
            new NumberingId { Val = numberingId }
        );

        var paraProps = new ParagraphProperties(numberingProps);
        if (!string.IsNullOrEmpty(styleId))
        {
            paraProps.ParagraphStyleId = new ParagraphStyleId { Val = styleId };
        }

        if (indentTwips.HasValue)
        {
            paraProps.Indentation = new Indentation { Left = indentTwips.Value.ToString() };
        }

        return new Paragraph(
            paraProps,
            new Run(new Text(text))
        );
    }

    private static Paragraph CreateNumberedHeading(string text, int numberingId, int level = 0)
    {
        return new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading1" },
                new NumberingProperties(
                    new NumberingLevelReference { Val = level },
                    new NumberingId { Val = numberingId })),
            new Run(new Text(text)));
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
    public void PunctuationRule_ConsistentPunctuationWithSemicolons_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item;", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_ConsistentPunctuationWithCommas_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item,", 1),
            CreateListItem("Second item,", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_MixedPunctuation_ReturnsErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item,", 1),
            CreateListItem("Third item;", 1),
            CreateListItem("Fourth item.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("','", errors[0].Message);
        Assert.Contains("';'", errors[0].Message);
    }

    [Fact]
    public void PunctuationRule_ListParagraphStyle_IsCheckedAsListItem()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, styleId: "ListParagraph"),
            CreateListItem("Second item,", 1, styleId: "ListParagraph"),
            CreateListItem("Third item.", 1, styleId: "ListParagraph")
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("','", errors[0].Message);
        Assert.Contains("';'", errors[0].Message);
    }

    [Fact]
    public void PunctuationRule_LastItemNotEndingWithPeriod_ReturnsError()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item;", 1),
            CreateListItem("Third item;", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Last list item should end with period", errors[0].Message);
    }

    [Fact]
    public void PunctuationRule_MiddleItemMissingPunctuation_ReturnsError()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("no punctuation", errors[0].Message);
        Assert.Contains("';'", errors[0].Message);
    }

    [Fact]
    public void PunctuationRule_SingleItemList_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("Only item.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_FirstItemNoPunctuation_MiddleItemsMustMatch()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item", 1),
            CreateListItem("Second item;", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("';'", errors[0].Message);
        Assert.Contains("no punctuation", errors[0].Message);
    }

    [Fact]
    public void PunctuationRule_AllItemsNoPunctuation_LastMustEndWithPeriod()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item", 1),
            CreateListItem("Second item", 1),
            CreateListItem("Third item", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Last list item should end with period", errors[0].Message);
    }

    [Fact]
    public void IndentationRule_ConsistentIndentation_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Second item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Third item.", 1, level: 0, indentTwips: 720)
        );

        var errors = _indentationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void IndentationRule_InconsistentIndentation_ReturnsError()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Second item;", 1, level: 0, indentTwips: 1440),
            CreateListItem("Third item.", 1, level: 0, indentTwips: 720)
        );

        var errors = _indentationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("inconsistent indentation", errors[0].Message);
    }

    [Fact]
    public void IndentationRule_ListParagraphStyle_IsCheckedAsListItem()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720, styleId: "ListParagraph"),
            CreateListItem("Second item;", 1, level: 0, indentTwips: 1440, styleId: "ListParagraph"),
            CreateListItem("Third item.", 1, level: 0, indentTwips: 720, styleId: "ListParagraph")
        );

        var errors = _indentationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("inconsistent indentation", errors[0].Message);
    }

    [Fact]
    public void IndentationRule_DifferentLevelsWithDifferentIndentation_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Sub item;", 1, level: 1, indentTwips: 1440),
            CreateListItem("Second item.", 1, level: 0, indentTwips: 720)
        );

        var errors = _indentationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_IgnoresIndentationDifferences()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Second item.", 1, level: 0, indentTwips: 1440)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void IndentationRule_IgnoresPunctuationDifferences()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, level: 0, indentTwips: 720),
            CreateListItem("Second item,", 1, level: 0, indentTwips: 720),
            CreateListItem("Third item.", 1, level: 0, indentTwips: 720)
        );

        var errors = _indentationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_TwoSeparateLists_CheckedIndependently()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("List1 item1;", 1),
            CreateListItem("List1 item2.", 1),
            new Paragraph(new Run(new Text("Normal paragraph between lists"))),
            CreateListItem("List2 item1,", 2),
            CreateListItem("List2 item2.", 2)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_NumberedHeadings_AreNotCheckedAsListItems()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateNumberedHeading("Chapter one", 1),
            CreateNumberedHeading("Chapter two", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_ExcludedStylePattern_AreNotCheckedAsListItems()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("Caption one;", 1, styleId: "Caption"),
            CreateListItem("Caption two;", 1, styleId: "Caption")
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_NumberedHeading_BreaksAdjacentLists()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First list item;", 1),
            CreateListItem("First list end.", 1),
            CreateNumberedHeading("Chapter heading", 1),
            CreateListItem("Second list item;", 1),
            CreateListItem("Second list end.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_TwoListsBothWithErrors_ReportsAllErrors()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("List1 item1;", 1),
            CreateListItem("List1 item2;", 1),
            new Paragraph(new Run(new Text("Normal paragraph"))),
            CreateListItem("List2 item1,", 2),
            CreateListItem("List2 item2,", 2)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void PunctuationRule_EmptyListItem_HandledGracefully()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("", 1),
            CreateListItem("Third item.", 1)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("no punctuation", errors[0].Message);
        Assert.Contains("';'", errors[0].Message);
    }

    [Fact]
    public void PunctuationRule_NoLists_ReturnsNoErrors()
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

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void PunctuationRule_NestedListLevels_CheckedSeparately()
    {
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("Main item 1:", 1, level: 0),
            CreateListItem("Sub item a;", 1, level: 1),
            CreateListItem("Sub item b.", 1, level: 1),
            CreateListItem("Main item 2.", 1, level: 0)
        );

        var errors = _punctuationRule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.NotNull(errors);
    }
}
