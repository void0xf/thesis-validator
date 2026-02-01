using backend.Models;
using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;

namespace backend.Tests.Rules;

public class ParagraphSpacingRuleTests
{
    private readonly ParagraphSpacingRule _rule = new();

    private static UniversityConfig CreateConfig(params int[] allowedSpacingPts)
    {
        return new UniversityConfig
        {
            Formatting = new FormattingConfig
            {
                Layout = new LayoutConfig
                {
                    ParagraphSpacingRule = allowedSpacingPts.ToList()
                }
            }
        };
    }

    private static InMemoryDocx CreateDocxWithSpacing(params int[] spacingAfterTwips)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var twips in spacingAfterTwips)
        {
            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { After = twips.ToString() }
                ),
                new Run(new Text("Test paragraph"))
            );
            mainPart.Document.Body!.Append(paragraph);
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    [Fact]
    public void Validate_ParagraphWithCorrectSpacing0pt_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithSpacing(0);
        var config = CreateConfig(0);

        var errors = _rule.Validate(docx.Document, config, null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ParagraphWithCorrectSpacing6pt_ReturnsNoErrors()
    {
        using var docx = CreateDocxWithSpacing(120);
        var config = CreateConfig(6);

        var errors = _rule.Validate(docx.Document, config, null).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ParagraphWithWrongSpacing_ReturnsError()
    {
        using var docx = CreateDocxWithSpacing(200);
        var config = CreateConfig(0, 6);

        var errors = _rule.Validate(docx.Document, config, null).ToList();

        Assert.Single(errors);
        Assert.Contains("10", errors[0].Message);
    }

    [Fact]
    public void Validate_MultipleParagraphs_ReturnsErrorsForIncorrectOnes()
    {
        using var docx = CreateDocxWithSpacing(0, 120, 200, 120);
        var config = CreateConfig(0, 6);

        var errors = _rule.Validate(docx.Document, config, null).ToList();

        Assert.Single(errors);
    }

    [Fact]
    public void Validate_NoSpacingSet_TreatedAs0Twips()
    {
        var stream = new MemoryStream();
        var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            new Paragraph(new Run(new Text("No spacing set")))
        ));
        mainPart.Document.Save();
        using var docx = new InMemoryDocx(wordDoc, stream);

        var config = CreateConfig(0, 6);

        var errors = _rule.Validate(docx.Document, config, null).ToList();

        Assert.Empty(errors);
    }
}
