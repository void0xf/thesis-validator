using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;

namespace backend.Tests.Rules;

public class LineSpacingDependencyRuleTests
{
    private readonly LineSpacingDependencyRule _rule = new();

    private static UniversityConfig CreateConfig() => new();

    private static InMemoryDocx CreateDocxWithLineSpacing(
        int? lineValue,
        LineSpacingRuleValues? lineRule,
        int? beforeTwips,
        int? afterTwips,
        string? styleId = null)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var spacing = new SpacingBetweenLines();

        if (lineValue.HasValue)
            spacing.Line = lineValue.Value.ToString();

        if (lineRule.HasValue)
            spacing.LineRule = lineRule.Value;

        if (beforeTwips.HasValue)
            spacing.Before = beforeTwips.Value.ToString();

        if (afterTwips.HasValue)
            spacing.After = afterTwips.Value.ToString();

        var paragraphProperties = new ParagraphProperties(spacing);
        if (!string.IsNullOrEmpty(styleId))
        {
            paragraphProperties.ParagraphStyleId = new ParagraphStyleId { Val = styleId };
        }

        var paragraph = new Paragraph(
            paragraphProperties,
            new Run(new Text("Test paragraph")));

        mainPart.Document.Body!.Append(paragraph);
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    [Fact]
    public void LineSpacing15_WithSpacingAfter_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, null, 4000);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void LineSpacing15_WithSpacingBefore_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, 120, 0);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void LineSpacing15_WithZeroSpacing_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, 0, 0);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void LineSpacing15_WithNullSpacing_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, null, null);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void SingleLineSpacing_WithSpacingAfter_ReturnsError()
    {
        using var docx = CreateDocxWithLineSpacing(240, LineSpacingRuleValues.Auto, null, 120);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("line spacing must be 1.5", errors[0].Message);
        Assert.Contains("Found: 1.0", errors[0].Message);
    }

    [Fact]
    public void DoubleLineSpacing_WithSpacingAfter_ReturnsError()
    {
        using var docx = CreateDocxWithLineSpacing(480, LineSpacingRuleValues.Auto, null, 120);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Found: 2.0", errors[0].Message);
    }

    [Fact]
    public void LineSpacing15_With6ptSpacing_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, null, 120);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void MissingLineSpacing_ReturnsError()
    {
        using var docx = CreateDocxWithLineSpacing(null, null, null, null);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Found: not set", errors[0].Message);
    }

    [Fact]
    public void ExactLineRule_ReturnsError()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Exact, null, null);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Exact line rule", errors[0].Message);
    }

    [Fact]
    public void ExcludedStylePattern_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(240, LineSpacingRuleValues.Auto, null, 120, "Caption");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ExcludedListStyleParagraphsAreSkipped()
    {
        using var docx = CreateDocxWithLineSpacing(240, LineSpacingRuleValues.Auto, null, 120, "ListParagraph");

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }
}
