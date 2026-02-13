using backend.Tests.Helpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit.Abstractions;

namespace backend.Tests.Exploratory;

public class LineSpacingExplorationTests
{
    private readonly ITestOutputHelper _output;

    public LineSpacingExplorationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Explore_LineSpacing_Values()
    {
        using var doc = DocxTestHelper.OpenDocxAsRead("WrongIndentANDSpacing/hello - Copy.docx");
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
        {
            _output.WriteLine("Body is null");
            return;
        }

        int paragraphIndex = 0;
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;
            var spacing = paragraph.ParagraphProperties?.SpacingBetweenLines;

            var text = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
            var preview = text.Length > 50 ? text.Substring(0, 50) + "..." : text;

            _output.WriteLine($"=== Paragraph {paragraphIndex}: \"{preview}\" ===");

            if (spacing == null)
            {
                _output.WriteLine("  No SpacingBetweenLines element");
                continue;
            }

            _output.WriteLine($"  Line: {spacing.Line?.Value ?? "null"}");
            _output.WriteLine($"  LineRule: {spacing.LineRule?.Value.ToString() ?? "null"}");
            _output.WriteLine($"  Before: {spacing.Before?.Value ?? "null"}");
            _output.WriteLine($"  After: {spacing.After?.Value ?? "null"}");
            _output.WriteLine($"  BeforeLines: {spacing.BeforeLines?.Value.ToString() ?? "null"}");
            _output.WriteLine($"  AfterLines: {spacing.AfterLines?.Value.ToString() ?? "null"}");
        }
    }

    [Fact]
    public void Explore_Output_Docx()
    {
        // Direct path to fixtures hello.docx
        using var doc = WordprocessingDocument.Open(
            @"C:\Users\envv\Documents\GitHub\thesis-validator\backend.Tests\Fixtures\hello.docx", false);
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
        {
            _output.WriteLine("Body is null");
            return;
        }

        int paragraphIndex = 0;
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;
            var spacing = paragraph.ParagraphProperties?.SpacingBetweenLines;
            var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

            var text = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
            var preview = text.Length > 50 ? text.Substring(0, 50) + "..." : text;

            _output.WriteLine($"=== Paragraph {paragraphIndex}: \"{preview}\" ===");
            _output.WriteLine($"  StyleId: {styleId ?? "null (uses default)"}");

            if (spacing == null)
            {
                _output.WriteLine("  No SpacingBetweenLines element");
                continue;
            }

            _output.WriteLine($"  Line: {spacing.Line?.Value ?? "null"}");
            _output.WriteLine($"  LineRule: {spacing.LineRule?.Value.ToString() ?? "null"}");
            _output.WriteLine($"  LineRule HasValue: {spacing.LineRule?.HasValue}");
            _output.WriteLine($"  LineRule == Auto: {spacing.LineRule?.Value == LineSpacingRuleValues.Auto}");
            _output.WriteLine($"  Before: {spacing.Before?.Value ?? "null"}");
            _output.WriteLine($"  After: {spacing.After?.Value ?? "null"}");
        }
    }

    [Fact]
    public void Explore_Styles_In_HelloDocx()
    {
        using var doc = WordprocessingDocument.Open(
            @"C:\Users\envv\Documents\GitHub\thesis-validator\backend.Tests\Fixtures\hello.docx", false);

        var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
        if (styles == null)
        {
            _output.WriteLine("No styles found");
            return;
        }

        _output.WriteLine("=== Document Defaults ===");
        var docDefaults = styles.DocDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle?.SpacingBetweenLines;
        if (docDefaults != null)
        {
            _output.WriteLine($"  DocDefaults Line: {docDefaults.Line?.Value ?? "null"}");
            _output.WriteLine($"  DocDefaults LineRule: {docDefaults.LineRule?.Value.ToString() ?? "null"}");
            _output.WriteLine($"  DocDefaults Before: {docDefaults.Before?.Value ?? "null"}");
            _output.WriteLine($"  DocDefaults After: {docDefaults.After?.Value ?? "null"}");
        }
        else
        {
            _output.WriteLine("  No DocDefaults SpacingBetweenLines");
        }

        _output.WriteLine("\n=== Paragraph Styles ===");
        foreach (var style in styles.Elements<Style>().Where(s => s.Type?.Value == StyleValues.Paragraph))
        {
            var spacing = style.StyleParagraphProperties?.SpacingBetweenLines;
            _output.WriteLine($"\nStyle: {style.StyleId?.Value} (Default: {style.Default?.Value})");
            _output.WriteLine($"  BasedOn: {style.BasedOn?.Val?.Value ?? "none"}");
            if (spacing != null)
            {
                _output.WriteLine($"  Line: {spacing.Line?.Value ?? "null"}");
                _output.WriteLine($"  LineRule: {spacing.LineRule?.Value.ToString() ?? "null"}");
                _output.WriteLine($"  Before: {spacing.Before?.Value ?? "null"}");
                _output.WriteLine($"  After: {spacing.After?.Value ?? "null"}");
            }
            else
            {
                _output.WriteLine("  No SpacingBetweenLines in this style");
            }
        }
    }
}
