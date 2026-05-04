using backend.Annotation;
using backend.Application.Validation;
using backend.Rules;
using backend.Tests.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public sealed class MissingTextBoxCaptionRuleTests
{
    private readonly MissingTextBoxCaptionRule _rule = new();

    [Fact]
    public void Validate_WhenTextBoxHasNoCaption_ReturnsWarningProblem()
    {
        var problems = Validate(TextBoxParagraph("Important note"));

        var problem = Assert.Single(problems);
        Assert.Contains("no caption", problem.Message);
        Assert.Equal(RuleSeverity.Warning, _rule.Descriptor.DefaultSeverity);
        Assert.Equal(ParagraphIndexKind.BodyElement, problem.ParagraphIndexKind);
        Assert.IsType<ParagraphAnnotationTarget>(problem.AnnotationTarget);
    }

    [Fact]
    public void Validate_WhenNestedTextBoxHasNoCaption_ReturnsWarningProblem()
    {
        var problems = Validate(
            new SdtBlock(
                new SdtContentBlock(
                    TextBoxParagraph("Nested note"))));

        var problem = Assert.Single(problems);
        Assert.Contains("no caption", problem.Message);
        Assert.Equal("Nested note", problem.Location.Text);
    }

    [Fact]
    public void Validate_WhenCaptionImmediatelyAboveTextBox_DoesNotReport()
    {
        var problems = Validate(
            Paragraph("Rysunek 1. Opis ramki"),
            TextBoxParagraph("Important note"));

        Assert.Empty(problems);
    }

    [Theory]
    [InlineData("Tekst 1. Opis ramki")]
    [InlineData("Text 1. Text box caption")]
    [InlineData("Text box 1. Caption")]
    public void Validate_WhenTextCaptionImmediatelyAboveTextBox_DoesNotReport(string caption)
    {
        var problems = Validate(
            Paragraph(caption),
            TextBoxParagraph("Important note"));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WhenCaptionImmediatelyBelowTextBox_DoesNotReport()
    {
        var problems = Validate(
            TextBoxParagraph("Important note"),
            Paragraph("Figure 1. Text box caption"));

        Assert.Empty(problems);
    }

    [Theory]
    [InlineData("Tekst 1. Opis ramki")]
    [InlineData("Text 1. Text box caption")]
    [InlineData("Text box 1. Caption")]
    public void Validate_WhenTextCaptionImmediatelyBelowTextBox_DoesNotReport(string caption)
    {
        var problems = Validate(
            TextBoxParagraph("Important note"),
            Paragraph(caption));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WhenAdjacentCaptionStyleParagraphIsEmpty_ReturnsWarningProblem()
    {
        var problems = Validate(
            TextBoxParagraph("Important note"),
            Paragraph(string.Empty, "Caption"));

        var problem = Assert.Single(problems);
        Assert.Contains("no caption", problem.Message);
    }

    [Fact]
    public void Validate_WhenParagraphDoesNotContainTextBox_DoesNotReport()
    {
        var problems = Validate(Paragraph("Regular body text"));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ProblemLocationUsesBodyParagraphIndex()
    {
        var problems = Validate(
            Paragraph("Intro"),
            TextBoxParagraph("Important note"));

        var problem = Assert.Single(problems);
        Assert.Equal(2, problem.Location.Paragraph);
        Assert.Equal("Important note", problem.Location.Text);
    }

    [Fact]
    public void AnnotationApplicator_WhenProblemTargetsTextBoxParagraph_AddsCommentToParagraph()
    {
        using var docx = CreateInMemoryDocx(TextBoxParagraph("Important note"));
        var context = new RuleContext
        {
            RawDocument = docx.Document,
            Content = new DocumentContent()
        };
        var problem = Assert.Single(new MissingTextBoxCaptionRule()
            .Validate(context, new MissingTextBoxCaptionRuleOptions()));

        new AnnotationApplicator().Apply(
            docx.Document,
            [new RuleExecution(new ValidationIssue(), problem)]);

        var comments = docx.Document.MainDocumentPart?.WordprocessingCommentsPart?.Comments;
        Assert.NotNull(comments);
        Assert.Contains(
            comments!.Elements<Comment>(),
            comment => comment.InnerText.Contains("Text box has no caption"));

        var paragraph = Assert.Single(docx.Document.MainDocumentPart!.Document!.Body!.Elements<Paragraph>());
        Assert.NotEmpty(paragraph.Descendants<CommentReference>());
    }

    [Fact]
    public void Validate_RuleDescriptorUsesWarningSeverity()
    {
        Assert.Equal(MissingTextBoxCaptionRule.RuleId, _rule.Descriptor.Name);
        Assert.Equal(RuleSeverity.Warning, _rule.Descriptor.DefaultSeverity);
    }

    private static IReadOnlyList<RuleProblem> Validate(params OpenXmlElement[] bodyChildren)
    {
        using var docx = CreateInMemoryDocx(bodyChildren);
        var context = new RuleContext
        {
            RawDocument = docx.Document,
            Content = new DocumentContent()
        };

        return new MissingTextBoxCaptionRule()
            .Validate(context, new MissingTextBoxCaptionRuleOptions())
            .ToList();
    }

    private static InMemoryDocx CreateInMemoryDocx(params OpenXmlElement[] bodyChildren)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var child in bodyChildren)
        {
            mainPart.Document.Body!.Append(child.CloneNode(deep: true));
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    private static Paragraph Paragraph(string text, string? styleId = null)
    {
        var paragraph = new Paragraph(
            new Run(
                new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

        if (!string.IsNullOrWhiteSpace(styleId))
        {
            paragraph.PrependChild(new ParagraphProperties(
                new ParagraphStyleId { Val = styleId }));
        }

        return paragraph;
    }

    private static Paragraph TextBoxParagraph(string text)
    {
        return new Paragraph(
            new Run(
                new TextBoxContent(
                    new Paragraph(
                        new Run(
                            new Text(text) { Space = SpaceProcessingModeValues.Preserve })))));
    }

}
