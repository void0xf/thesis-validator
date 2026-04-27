using Backend.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using backend.Services.Structure;

namespace backend.Tests.Services;

public class CaptionDetectionServiceTests
{
    [Theory]
    [InlineData("Caption", true)]
    [InlineData("Legenda", true)]
    [InlineData("Legend", true)]
    [InlineData("Normal", false)]
    [InlineData("Normalny", false)]
    [InlineData(null, false)]
    public void UsesDedicatedCaptionStyle_ClassifiesCaptionStyle(string? styleId, bool expected)
    {
        var paragraph = CreateParagraph(styleId, "Figure caption");

        var result = CaptionDetectionService.UsesDedicatedCaptionStyle(paragraph);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetFirstTextRun_ReturnsFirstNonWhitespaceRun()
    {
        var paragraph = new Paragraph(
            new Run(new Text("   ")),
            new Run(new Text("Caption text")));

        var run = CaptionDetectionService.GetFirstTextRun(paragraph, new UniversityConfig());

        Assert.Equal("Caption text", run!.GetFirstChild<Text>()!.Text);
    }

    [Theory]
    [InlineData("Rys 1 Schemat architektury systemu", true)]
    [InlineData("Rys 1", true)]
    [InlineData("Rys. 1 Schemat architektury systemu", true)]
    [InlineData("Rys. 1", true)]
    [InlineData("Rysunek 1 Schemat architektury systemu", true)]
    [InlineData("Rysunek 1", true)]
    [InlineData("Rys. 2.1", true)]
    [InlineData("Diagram 1 Schemat architektury systemu", false)]
    [InlineData("Rysunek: 1 Schemat architektury systemu", false)]
    public void HasValidFigureCaptionFormat_ClassifiesSupportedLabels(string text, bool expected)
    {
        Assert.Equal(expected, CaptionDetectionService.HasValidFigureCaptionFormat(text));
    }

    private static Paragraph CreateParagraph(string? styleId, string text)
    {
        var paragraph = new Paragraph(new Run(new Text(text)));
        if (styleId is not null)
        {
            paragraph.PrependChild(
                new ParagraphProperties(new ParagraphStyleId { Val = styleId }));
        }

        return paragraph;
    }
}
