using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;
using ThesisValidator.Rules;
using backend.Services.Analysis;

namespace backend.Tests.Rules;

public class TocRuleTests
{
    [Fact]
    public void Validate_WhenAutomaticTocExists_ReturnsNoIssue()
    {
        using var docx = CreateDocxWithAutomaticToc();

        var results = new TocRule()
            .Validate(docx.Document, new UniversityConfig())
            .ToList();

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Spis tre\u015bci")]
    [InlineData("Spis tresci")]
    [InlineData("SPIS TRE\u015aCI")]
    [InlineData("Spis   Tre\u015bci:")]
    [InlineData("Spis rzeczy.")]
    [InlineData("Table of contents")]
    [InlineData("Contents")]
    public void Validate_WhenManualTocHeadingExists_ReturnsStructureWarning(string heading)
    {
        using var docx = CreateDocx(heading, "Chapter 1");

        var result = Assert.Single(new TocRule().Validate(docx.Document, new UniversityConfig()));

        Assert.False(result.IsError);
        Assert.Equal(TocRule.ManualTableOfContentsRuleName, result.RuleName);
        Assert.Equal("Structure", result.Category);
        Assert.Equal(1, result.Location.Paragraph);
        Assert.Contains("no automatic Word TOC field", result.Message);
    }

    [Fact]
    public void Validate_WhenNoAutomaticOrManualTocExists_KeepsMissingTocError()
    {
        using var docx = CreateDocx("Chapter 1", "Body text");

        var result = Assert.Single(new TocRule().Validate(docx.Document, new UniversityConfig()));

        Assert.True(result.IsError);
        Assert.Equal(nameof(FormattingConfig.CheckTableOfContents), result.RuleName);
        Assert.Equal("Document is missing a Table of Contents.", result.Message);
    }

    [Fact]
    public void Validate_WhenSkipBeforeTocEnabled_IgnoresIssuesBeforeDetectedToc()
    {
        using var stream = CreateDocxStream(
            "Before  issue",
            "Spis tre\u015bci",
            "After  issue");

        var config = new UniversityConfig();
        config.Formatting.SkipBeforeTableOfContents = true;

        var service = new ThesisValidatorService(new IValidationRule[] { new SingleSpaceRule() });

        var results = service.Validate(stream, config).Results.ToList();

        var result = Assert.Single(results);
        Assert.Contains("After", result.Location.Text);
    }

    private static InMemoryDocx CreateDocx(params string[] paragraphTexts)
    {
        var stream = CreateDocxStream(paragraphTexts);
        var doc = WordprocessingDocument.Open(stream, true);
        return new InMemoryDocx(doc, stream);
    }

    private static InMemoryDocx CreateDocxWithAutomaticToc()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        mainPart.Document.Body!.Append(
            new Paragraph(
                new Run(new FieldCode("TOC \\o \"1-3\" \\h \\z \\u"))));
        mainPart.Document.Body.Append(new Paragraph(new Run(new Text("Chapter 1"))));
        mainPart.Document.Save();

        return new InMemoryDocx(doc, stream);
    }

    private static MemoryStream CreateDocxStream(params string[] paragraphTexts)
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            foreach (var paragraphText in paragraphTexts)
            {
                mainPart.Document.Body!.Append(
                    new Paragraph(new Run(new Text(paragraphText))));
            }

            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class InMemoryDocx : IDisposable
    {
        public WordprocessingDocument Document { get; }
        private readonly MemoryStream _stream;

        public InMemoryDocx(WordprocessingDocument document, MemoryStream stream)
        {
            Document = document;
            _stream = stream;
        }

        public void Dispose()
        {
            Document.Dispose();
            _stream.Dispose();
        }
    }
}
