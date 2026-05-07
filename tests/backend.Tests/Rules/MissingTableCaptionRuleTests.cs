using backend.Annotation;
using backend.Application.Validation;
using backend.DocumentProcessing.Context;
using backend.Rules;
using backend.Tests.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public sealed class MissingTableCaptionRuleTests
{
    private readonly MissingTableCaptionRule _rule = new();

    [Fact]
    public void Validate_WhenRealTableHasNoCaption_ReturnsProblem()
    {
        var problems = Validate(RealTable());

        var problem = Assert.Single(problems);
        Assert.Contains("no caption", problem.Message);
        Assert.Equal("[Table]", problem.Location.Text);
        Assert.Equal(ParagraphIndexKind.BodyElement, problem.ParagraphIndexKind);
        Assert.IsType<TableAnnotationTarget>(problem.AnnotationTarget);
    }

    [Fact]
    public void Validate_WhenCaptionImmediatelyAboveTable_DoesNotReport()
    {
        var problems = Validate(
            Paragraph("Tabela 1. Wyniki"),
            RealTable());

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WhenCaptionImmediatelyBelowTable_DoesNotReportMissingCaption()
    {
        var problems = Validate(
            RealTable(),
            Paragraph("Table 1. Results"));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WhenOpenXmlTableHasNoCells_DoesNotTreatItAsRealTable()
    {
        var problems = Validate(new Table());

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WhenTableIsBeforeTableOfContents_DoesNotReport()
    {
        var problems = ValidateWithSkipBeforeTableOfContents(
            RealTable(),
            Paragraph("Spis tresci"),
            Paragraph("Introduction"));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ProblemLocationUsesPreviousBodyParagraphIndex()
    {
        var problems = Validate(
            Paragraph("Intro"),
            RealTable());

        var problem = Assert.Single(problems);
        Assert.Equal(1, problem.Location.Paragraph);
    }

    [Fact]
    public void Validate_RuleDescriptorUsesErrorSeverity()
    {
        Assert.Equal(MissingTableCaptionRule.RuleId, _rule.Descriptor.Name);
        Assert.Equal(RuleSeverity.Error, _rule.Descriptor.DefaultSeverity);
    }

    [Fact]
    public void AnnotationApplicator_WhenProblemTargetsTable_AddsCommentInsideTable()
    {
        using var docx = CreateInMemoryDocx(RealTable());
        var context = new RuleContext
        {
            RawDocument = docx.Document,
            Content = new DocumentContent()
        };
        var problem = Assert.Single(new MissingTableCaptionRule()
            .Validate(context, new MissingTableCaptionRuleOptions()));

        new AnnotationApplicator().Apply(
            docx.Document,
            [new RuleExecution(new ValidationIssue(), problem)]);

        var comments = docx.Document.MainDocumentPart?.WordprocessingCommentsPart?.Comments;
        Assert.NotNull(comments);
        Assert.Contains(
            comments!.Elements<Comment>(),
            comment => comment.InnerText.Contains("Table has no caption"));

        var table = Assert.Single(docx.Document.MainDocumentPart!.Document!.Body!.Elements<Table>());
        Assert.NotEmpty(table.Descendants<CommentReference>());
    }

    private static IReadOnlyList<RuleProblem> Validate(params OpenXmlElement[] bodyChildren)
    {
        using var docx = CreateInMemoryDocx(bodyChildren);
        var context = new RuleContext
        {
            RawDocument = docx.Document,
            Content = new DocumentContent()
        };

        return new MissingTableCaptionRule()
            .Validate(context, new MissingTableCaptionRuleOptions())
            .ToList();
    }

    private static IReadOnlyList<RuleProblem> ValidateWithSkipBeforeTableOfContents(
        params OpenXmlElement[] bodyChildren)
    {
        using var docx = CreateInMemoryDocx(bodyChildren);
        var context = new RuleContext
        {
            RawDocument = docx.Document,
            Content = new DocumentContent()
        };
        var skipResolver = new DocumentSkipResolver(Options.Create(new ValidationSkippingOptions
        {
            SkipBeforeTableOfContents = true
        }));

        return new MissingTableCaptionRule(skipResolver)
            .Validate(context, new MissingTableCaptionRuleOptions())
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

    private static Paragraph Paragraph(string text)
    {
        return new Paragraph(
            new Run(
                new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Table RealTable()
    {
        return new Table(
            new TableRow(
                new TableCell(
                    new Paragraph(
                        new Run(
                            new Text("Cell"))))));
    }
}
