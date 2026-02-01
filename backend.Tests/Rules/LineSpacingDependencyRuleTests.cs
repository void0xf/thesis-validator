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

    private static InMemoryDocx CreateDocxWithLineSpacing(int lineValue, LineSpacingRuleValues? lineRule, int? beforeTwips, int? afterTwips)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var spacing = new SpacingBetweenLines { Line = lineValue.ToString() };

        if (lineRule.HasValue)
            spacing.LineRule = lineRule.Value;

        if (beforeTwips.HasValue)
            spacing.Before = beforeTwips.Value.ToString();

        if (afterTwips.HasValue)
            spacing.After = afterTwips.Value.ToString();

        var paragraph = new Paragraph(
            new ParagraphProperties(spacing),
            new Run(new Text("Test paragraph"))
        );
        mainPart.Document.Body!.Append(paragraph);
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    [Fact]
    public void LineSpacing15_WithSpacingAfter_ReturnsError()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, null, 4000);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("1.5 line spacing", errors[0].Message);
        Assert.Contains("After=200", errors[0].Message);
    }

    [Fact]
    public void LineSpacing15_WithSpacingBefore_ReturnsError()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, 120, 0);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("Before=6", errors[0].Message);
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
    public void SingleLineSpacing_WithSpacingAfter_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(240, LineSpacingRuleValues.Auto, null, 120);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void DoubleLineSpacing_WithSpacingAfter_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithLineSpacing(480, LineSpacingRuleValues.Auto, null, 120);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void LineSpacing15_With6ptSpacing_ReturnsError()
    {
        using var docx = CreateDocxWithLineSpacing(360, LineSpacingRuleValues.Auto, null, 120);

        var errors = _rule.Validate(docx.Document, CreateConfig(), null).ToList();

        Assert.Single(errors);
        Assert.Contains("After=6", errors[0].Message);
    }

    [Fact]
    public void HelloDocx_DetectsLineSpacingViolation()
    {
        using var doc = WordprocessingDocument.Open(
            @"C:\Users\envv\Documents\GitHub\thesis-validator\backend.Tests\Fixtures\hello.docx", false);

        var errors = _rule.Validate(doc, CreateConfig(), null).ToList();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("1.5 line spacing"));
    }
}
