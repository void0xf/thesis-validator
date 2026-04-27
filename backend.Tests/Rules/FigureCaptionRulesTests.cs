using backend.Models;
using backend.Rules;
using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace backend.Tests.Rules;

public class FigureCaptionRulesTests
{
    private const string CaptionStyleId = "CustomLegend";

    private readonly MissingFigureCaptionRule _missingRule = new();
    private readonly FigureCaptionPositionRule _positionRule = new();
    private readonly FigureCaptionStyleRule _styleRule = new();
    private readonly FigureCaptionFormatRule _formatRule = new();
    private readonly FigureCaptionAutomaticNumberingRule _numberingRule = new();

    private static UniversityConfig CreateConfig() => new();

    [Fact]
    public void ImageWithCorrectAutomaticCaptionBelow_ReturnsNoFigureCaptionResults()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateAutomaticCaption("Rys. ", "1", " Schemat architektury systemu"));

        Assert.Empty(_missingRule.Validate(docx.Document, CreateConfig()));
        Assert.Empty(_positionRule.Validate(docx.Document, CreateConfig()));
        Assert.Empty(_styleRule.Validate(docx.Document, CreateConfig()));
        Assert.Empty(_formatRule.Validate(docx.Document, CreateConfig()));
        Assert.Empty(_numberingRule.Validate(docx.Document, CreateConfig()));
    }

    [Fact]
    public void ImageInsideTableWithoutCaption_ReturnsMissingCaptionError()
    {
        using var docx = CreateDocx(
            CreateSingleCellTable(
                CreatePictureParagraph()));
        var config = CreateConfig();

        var missing = _missingRule.Validate(docx.Document, config).ToList();

        Assert.Single(missing);
        Assert.Equal("MissingFigureCaptionRule", missing[0].RuleName);
    }

    [Fact]
    public void ImageInsideTableWithNormalTextCaptionBelow_ReturnsMissingAndStyleError()
    {
        using var docx = CreateDocx(
            CreateSingleCellTable(
                CreatePictureParagraph(),
                CreateCaptionParagraph("Rys. 1 Schemat architektury systemu", styleId: "Normal")));
        var config = CreateConfig();

        var missing = _missingRule.Validate(docx.Document, config).ToList();

        Assert.Single(missing);
        Assert.Equal("MissingFigureCaptionRule", missing[0].RuleName);
        Assert.Empty(_positionRule.Validate(docx.Document, config));
        Assert.Empty(_formatRule.Validate(docx.Document, config));
        Assert.Empty(_numberingRule.Validate(docx.Document, config));

        var styleErrors = _styleRule.Validate(docx.Document, config).ToList();

        Assert.NotEmpty(styleErrors);
        Assert.All(styleErrors, error => Assert.Equal("FigureCaptionStyleRule", error.RuleName));
    }

    [Fact]
    public void ImageInsideTableWithAutomaticCaptionBelow_DoesNotFailFormatRule()
    {
        using var docx = CreateDocx(
            CreateSingleCellTable(
                CreatePictureParagraph(),
                CreateAutomaticCaption("Rysunek ", "1", " Schemat architektury systemu")));
        var config = CreateConfig();

        Assert.Empty(_missingRule.Validate(docx.Document, config));
        Assert.Empty(_positionRule.Validate(docx.Document, config));
        Assert.Empty(_formatRule.Validate(docx.Document, config));
    }

    [Fact]
    public void ImageWithAutomaticToolbarCaptionWithoutDescription_DoesNotFailFormatRule()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateAutomaticCaption("Rysunek ", "1", string.Empty));
        var config = CreateConfig();

        Assert.Empty(_missingRule.Validate(docx.Document, config));
        Assert.Empty(_formatRule.Validate(docx.Document, config));
    }

    [Fact]
    public void ImageWithoutCaption_ReturnsOnlyMissingCaptionError()
    {
        using var docx = CreateDocx(CreatePictureParagraph());
        var config = CreateConfig();

        var missing = _missingRule.Validate(docx.Document, config).ToList();

        Assert.Single(missing);
        Assert.Equal("MissingFigureCaptionRule", missing[0].RuleName);
        Assert.Equal(ValidationSeverity.Error, missing[0].Severity);
        Assert.Empty(_positionRule.Validate(docx.Document, config));
        Assert.Empty(_styleRule.Validate(docx.Document, config));
        Assert.Empty(_formatRule.Validate(docx.Document, config));
        Assert.Empty(_numberingRule.Validate(docx.Document, config));
    }

    [Fact]
    public void ImageWithEmptyCaptionStyledParagraph_ReturnsOnlyMissingCaptionError()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph("   "));
        var config = CreateConfig();

        var missing = _missingRule.Validate(docx.Document, config).ToList();

        Assert.Single(missing);
        Assert.Equal("MissingFigureCaptionRule", missing[0].RuleName);
        Assert.Empty(_positionRule.Validate(docx.Document, config));
        Assert.Empty(_styleRule.Validate(docx.Document, config));
        Assert.Empty(_formatRule.Validate(docx.Document, config));
        Assert.Empty(_numberingRule.Validate(docx.Document, config));
    }

    [Fact]
    public void ImageWithManuallyTypedValidCaption_WarnsAboutManualNumberingOnly()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph("Rys. 1 Schemat architektury systemu"));
        var config = CreateConfig();

        Assert.Empty(_missingRule.Validate(docx.Document, config));
        Assert.Empty(_formatRule.Validate(docx.Document, config));

        var warnings = _numberingRule.Validate(docx.Document, config).ToList();

        Assert.Single(warnings);
        Assert.Equal("FigureCaptionAutomaticNumberingRule", warnings[0].RuleName);
        Assert.Equal(ValidationSeverity.Warning, warnings[0].Severity);
    }

    [Theory]
    [InlineData("Rys 1 Schemat architektury systemu")]
    [InlineData("Rysunek 1 Schemat architektury systemu")]
    public void ImageWithManuallyTypedValidCaptionUsingSupportedLabels_WarnsAboutManualNumberingOnly(
        string captionText)
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph(captionText));
        var config = CreateConfig();

        Assert.Empty(_missingRule.Validate(docx.Document, config));
        Assert.Empty(_formatRule.Validate(docx.Document, config));

        var warnings = _numberingRule.Validate(docx.Document, config).ToList();

        Assert.Single(warnings);
        Assert.Equal("FigureCaptionAutomaticNumberingRule", warnings[0].RuleName);
        Assert.Equal(ValidationSeverity.Warning, warnings[0].Severity);
    }

    [Fact]
    public void CaptionAboveImage_ReturnsPositionErrorWithoutMissingCaptionError()
    {
        using var docx = CreateDocx(
            CreateAutomaticCaption("Rys. ", "1", " Schemat architektury systemu"),
            CreatePictureParagraph());
        var config = CreateConfig();

        Assert.Empty(_missingRule.Validate(docx.Document, config));

        var errors = _positionRule.Validate(docx.Document, config).ToList();

        Assert.Single(errors);
        Assert.Equal("FigureCaptionPositionRule", errors[0].RuleName);
        Assert.Equal(ValidationSeverity.Error, errors[0].Severity);
        Assert.Contains("below", errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FigureBeforeAnotherFigure_DoesNotTreatLaterCaptionAsSeparatedCaption()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreatePictureParagraph(),
            CreateCaptionParagraph("Rys. 2 Schemat architektury systemu"));
        var config = CreateConfig();

        var missing = _missingRule.Validate(docx.Document, config).ToList();
        var position = _positionRule.Validate(docx.Document, config).ToList();

        Assert.Single(missing);
        Assert.Equal("MissingFigureCaptionRule", missing[0].RuleName);
        Assert.Empty(position);
    }

    [Fact]
    public void ZeroWidthSpacingBetweenFigureAndCaption_DoesNotTriggerPositionError()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph("\u200B"),
            CreateCaptionParagraph("Rys. 1 Schemat architektury systemu"));
        var config = CreateConfig();

        Assert.Empty(_missingRule.Validate(docx.Document, config));
        Assert.Empty(_positionRule.Validate(docx.Document, config));
    }

    [Fact]
    public void CaptionWithWrongLabel_ReturnsFormatErrorWithoutMissingCaptionError()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph("Obrazek 1 Schemat architektury systemu"));
        var config = CreateConfig();

        Assert.Empty(_missingRule.Validate(docx.Document, config));

        var errors = _formatRule.Validate(docx.Document, config).ToList();

        Assert.Single(errors);
        Assert.Equal("FigureCaptionFormatRule", errors[0].RuleName);
        Assert.Equal(ValidationSeverity.Error, errors[0].Severity);
    }

    [Fact]
    public void CaptionWithWrongStyle_ReturnsStyleError()
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph(
                "Rys. 1 Schemat architektury systemu",
                styleId: "Normal",
                includeDirectCaptionFormatting: true));

        var errors = _styleRule.Validate(docx.Document, CreateConfig()).ToList();

        Assert.Single(errors);
        Assert.Equal("FigureCaptionStyleRule", errors[0].RuleName);
        Assert.Equal(ValidationSeverity.Error, errors[0].Severity);
        Assert.Contains("Caption style", errors[0].Message);
    }

    [Fact]
    public void TextBoxOnlyDrawing_DoesNotRequireFigureCaption()
    {
        using var docx = CreateDocx(CreateTextBoxOnlyParagraph());

        var errors = _missingRule.Validate(docx.Document, CreateConfig()).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ChartDrawing_IsTreatedAsFigureCandidate()
    {
        using var docx = CreateDocx(CreateChartParagraph());

        var errors = _missingRule.Validate(docx.Document, CreateConfig()).ToList();

        Assert.Single(errors);
        Assert.Equal("MissingFigureCaptionRule", errors[0].RuleName);
    }

    [Theory]
    [InlineData("Rys. 1 Schemat architektury systemu")]
    [InlineData("Rys. 1")]
    [InlineData("Rys 1 Schemat architektury systemu")]
    [InlineData("Rys 1")]
    [InlineData("Rys. 1: Schemat architektury systemu")]
    [InlineData("Rysunek 1 Schemat architektury systemu")]
    [InlineData("Rysunek 1")]
    [InlineData("Rys. 2.1 Diagram klas")]
    [InlineData("Rys. 2.1")]
    public void AcceptedVisibleCaptionFormats_PassFormatRule(string captionText)
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph(captionText));

        var errors = _formatRule.Validate(docx.Document, CreateConfig()).ToList();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Diagram 1 Schemat architektury systemu")]
    [InlineData("Rysunek: 1 Schemat architektury systemu")]
    public void InvalidVisibleCaptionFormats_FailFormatRule(string captionText)
    {
        using var docx = CreateDocx(
            CreatePictureParagraph(),
            CreateCaptionParagraph(captionText));

        var errors = _formatRule.Validate(docx.Document, CreateConfig()).ToList();

        Assert.Single(errors);
        Assert.Equal("FigureCaptionFormatRule", errors[0].RuleName);
    }

    private static InMemoryDocx CreateDocx(params OpenXmlElement[] bodyChildren)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        AddStyles(mainPart);

        foreach (var child in bodyChildren)
        {
            mainPart.Document.Body!.Append(child);
        }

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    private static void AddStyles(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style(
                new StyleName { Val = "Normal" })
            {
                Type = StyleValues.Paragraph,
                Default = true,
                StyleId = "Normal"
            },
            new Style(
                new StyleName { Val = "Legenda" },
                new StyleParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new Indentation { Left = "0", FirstLine = "0" }),
                new StyleRunProperties(
                    new FontSize { Val = "22" }))
            {
                Type = StyleValues.Paragraph,
                StyleId = CaptionStyleId
            });
    }

    private static Paragraph CreatePictureParagraph()
    {
        return new Paragraph(new Run(CreatePictureDrawing()));
    }

    private static Table CreateSingleCellTable(params OpenXmlElement[] cellContent)
    {
        var cell = new TableCell();
        foreach (var element in cellContent)
        {
            cell.Append(element.CloneNode(true));
        }

        return new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil })),
            new TableRow(cell));
    }

    private static Paragraph CreateChartParagraph()
    {
        return new Paragraph(new Run(new Drawing(
            new DW.Inline(
                new A.Graphic(
                    new A.GraphicData(new C.ChartReference { Id = "rIdChart1" })
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
                    })))));
    }

    private static Paragraph CreateTextBoxOnlyParagraph()
    {
        return new Paragraph(new Run(new Drawing(
            new DW.Inline(
                new A.Graphic(
                    new A.GraphicData(
                        new A.TextBody(
                            new A.BodyProperties(),
                            new A.ListStyle(),
                            new A.Paragraph(
                                new A.Run(new A.Text("Only text")))))
                    {
                        Uri = "http://schemas.microsoft.com/office/word/2010/wordprocessingShape"
                    })))));
    }

    private static Drawing CreatePictureDrawing()
    {
        return new Drawing(
            new DW.Inline(
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture())
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                    })));
    }

    private static Paragraph CreateAutomaticCaption(
        string label,
        string number,
        string description)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = CaptionStyleId }),
            new Run(new Text(label) { Space = SpaceProcessingModeValues.Preserve }),
            new SimpleField(
                new Run(new Text(number)))
            {
                Instruction = "SEQ Rys \\* ARABIC"
            },
            new Run(new Text(description) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph CreateCaptionParagraph(
        string text,
        string styleId = CaptionStyleId,
        bool includeDirectCaptionFormatting = false)
    {
        var paragraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = styleId });
        var runProperties = new RunProperties();

        if (includeDirectCaptionFormatting)
        {
            paragraphProperties.Append(
                new Justification { Val = JustificationValues.Center },
                new Indentation { Left = "0", FirstLine = "0" });
            runProperties.Append(new FontSize { Val = "22" });
        }

        return new Paragraph(
            paragraphProperties,
            new Run(
                runProperties,
                new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }
}
