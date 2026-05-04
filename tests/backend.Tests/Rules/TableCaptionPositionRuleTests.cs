using backend.Rules;
using backend.Tests.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public sealed class TableCaptionPositionRuleTests
{
    private readonly TableCaptionPositionRule _rule = new();

    [Theory]
    [InlineData("Tabela 1. Wyniki")]
    [InlineData("Tab. 1. Wyniki")]
    [InlineData("Table 1. Results")]
    public void Validate_WhenCaptionImmediatelyBelowTable_ReturnsProblem(string captionText)
    {
        var problems = Validate(RealTable(), Paragraph(captionText));

        var problem = Assert.Single(problems);
        Assert.Contains("below the table", problem.Message);
        Assert.Equal(1, problem.Location.Paragraph);
        Assert.Equal(captionText, problem.Location.Text);
        Assert.Equal(ParagraphIndexKind.BodyElement, problem.ParagraphIndexKind);
        Assert.IsType<ParagraphAnnotationTarget>(problem.AnnotationTarget);
    }

    [Fact]
    public void Validate_WhenCaptionAboveTableIsValid_DoesNotReportCaptionBelow()
    {
        var problems = Validate(
            Paragraph("Tabela 1. Wyniki"),
            RealTable(),
            Paragraph("Table 1. Duplicate caption"));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WhenParagraphBelowTableIsNotCaption_DoesNotReport()
    {
        var problems = Validate(
            RealTable(),
            Paragraph("Source: own work"));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WhenOpenXmlTableHasNoCells_DoesNotTreatItAsRealTable()
    {
        var problems = Validate(
            new Table(),
            Paragraph("Table 1. Results"));

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ProblemLocationUsesBodyParagraphIndex()
    {
        var problems = Validate(
            Paragraph("Intro"),
            RealTable(),
            Paragraph("Table 1. Results"));

        var problem = Assert.Single(problems);
        Assert.Equal(2, problem.Location.Paragraph);
    }

    [Fact]
    public void Validate_RuleDescriptorUsesWarningSeverity()
    {
        Assert.Equal(TableCaptionPositionRule.RuleId, _rule.Descriptor.Name);
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

        return new TableCaptionPositionRule()
            .Validate(context, new TableCaptionPositionRuleOptions())
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
