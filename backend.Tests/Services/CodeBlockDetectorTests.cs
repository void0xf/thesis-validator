using backend.Services.CodeBlocks;
using backend.Tests.Helpers;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;

namespace backend.Tests.Services;

public class CodeBlockDetectorTests
{
    [Fact]
    public void Analyze_ParagraphEntirelyInConsolas_IsCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("public async Task<IActionResult> ValidateDocument() { return Ok(); }", "Consolas"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.True(result.IsCodeBlock);
    }

    [Fact]
    public void Analyze_ParagraphEntirelyInCourierNew_IsCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("SELECT * FROM Documents WHERE Id = @id", "Courier New"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.True(result.IsCodeBlock);
    }

    [Fact]
    public void Analyze_NormalProseInTimesNewRoman_IsNotCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("This paragraph contains normal prose.", "Times New Roman"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.False(result.IsCodeBlock);
    }

    [Fact]
    public void Analyze_InlineCodeIdentifiersBelowRatio_IsNotCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocxWithMultipleRuns(
            ("Metoda ", "Times New Roman"),
            ("ValidateDocument", "Consolas"),
            (" uzywa klasy ", "Times New Roman"),
            ("DocumentParser", "Consolas"),
            (" do walidacji pracy dyplomowej.", "Times New Roman"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.False(result.IsCodeBlock);
        Assert.True(result.CodeFontTextRatio < 0.7);
    }

    [Fact]
    public void Analyze_EightyPercentCodeFont_IsCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocxWithMultipleRuns(
            ("aaaaaaaa", "Consolas"),
            ("bb", "Times New Roman"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.True(result.IsCodeBlock);
        Assert.Equal(0.8, result.CodeFontTextRatio, precision: 3);
    }

    [Fact]
    public void Analyze_FortyPercentCodeFont_IsNotCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocxWithMultipleRuns(
            ("aaaa", "Consolas"),
            ("bbbbbb", "Times New Roman"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.False(result.IsCodeBlock);
        Assert.Equal(0.4, result.CodeFontTextRatio, precision: 3);
    }

    [Fact]
    public void Analyze_EmptyParagraph_IsNotCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("", "Consolas"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.False(result.IsCodeBlock);
        Assert.Equal(0, result.TotalTextLength);
    }

    [Fact]
    public void Analyze_RequireWholeParagraphMonospace_MixedHighRatioIsNotCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocxWithMultipleRuns(
            ("aaaaaaaa", "Consolas"),
            ("bb", "Times New Roman"));

        var result = AnalyzeFirstParagraph(docx, requireWholeParagraphMonospace: true);

        Assert.False(result.IsCodeBlock);
    }

    [Fact]
    public void Analyze_RequireWholeParagraphMonospace_WholeParagraphInCodeFontIsCodeBlock()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Console.WriteLine(\"ok\");", "Consolas"));

        var result = AnalyzeFirstParagraph(docx, requireWholeParagraphMonospace: true);

        Assert.True(result.IsCodeBlock);
    }

    [Fact]
    public void Analyze_FontMatchingIsCaseInsensitive()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Console.WriteLine(\"ok\");", "consolas"));

        var result = AnalyzeFirstParagraph(docx);

        Assert.True(result.IsCodeBlock);
        Assert.Equal("consolas", result.MatchedFont);
    }

    private static CodeBlockDetectionResult AnalyzeFirstParagraph(
        InMemoryDocx docx,
        bool requireWholeParagraphMonospace = false)
    {
        var detector = CreateDetector(requireWholeParagraphMonospace);
        var paragraph = docx.Document.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        return detector.Analyze(paragraph, docx.Document.MainDocumentPart!);
    }

    private static CodeBlockDetector CreateDetector(bool requireWholeParagraphMonospace = false)
    {
        return new CodeBlockDetector(Options.Create(new CodeBlockDetectionOptions
        {
            MinimumCodeFontTextRatio = 0.7,
            RequireWholeParagraphMonospace = requireWholeParagraphMonospace
        }));
    }
}
