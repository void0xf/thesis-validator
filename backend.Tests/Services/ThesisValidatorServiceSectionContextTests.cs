using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;

namespace backend.Tests.Services;

public class ThesisValidatorServiceSectionContextTests
{
    [Fact]
    public void Validate_UsesResultParagraphIndexKindForSectionContext()
    {
        using var stream = CreateDocxWithHeadingsAndTable();
        var service = new ThesisValidatorService(new IValidationRule[]
        {
            new BodyElementIndexedRule()
        });

        var result = Assert.Single(service.Validate(stream, new UniversityConfig()).Results);

        Assert.Equal("Heading 2", result.Location.Section);
    }

    private sealed class BodyElementIndexedRule : IValidationRule
    {
        public string Name => "BodyElementIndexedRule";

        public IEnumerable<ValidationResult> Validate(
            WordprocessingDocument doc,
            UniversityConfig config,
            DocumentCommentService? documentCommentService = null)
        {
            return
            [
                new ValidationResult
                {
                    RuleName = Name,
                    Severity = ValidationSeverity.Error,
                    ParagraphIndexKind = ParagraphIndexKind.BodyElement,
                    Location = new DocumentLocation
                    {
                        Paragraph = 4,
                        Text = "Body after H2"
                    }
                }
            ];
        }
    }

    private static MemoryStream CreateDocxWithHeadingsAndTable()
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles(
                new Style(new StyleParagraphProperties(new OutlineLevel { Val = 0 }))
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "Heading1",
                    StyleName = new StyleName { Val = "heading 1" }
                },
                new Style(new StyleParagraphProperties(new OutlineLevel { Val = 1 }))
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "Heading2",
                    StyleName = new StyleName { Val = "heading 2" }
                });

            var body = mainPart.Document.Body!;
            body.Append(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
                new Run(new Text("Heading 1"))));
            body.Append(new Paragraph(new Run(new Text("Body after H1"))));
            body.Append(new Table(
                new TableRow(
                    new TableCell(new Paragraph(new Run(new Text("Cell 1")))),
                    new TableCell(new Paragraph(new Run(new Text("Cell 2")))))));
            body.Append(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Heading2" }),
                new Run(new Text("Heading 2"))));
            body.Append(new Paragraph(new Run(new Text("Body after H2"))));

            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }
}
