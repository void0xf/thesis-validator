using backend.Models;
using backend.Services;
using backend.Tests.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Tests.Services;

public class SectionContextTests
{
    [Fact]
    public void PopulateSectionContext_SetsSectionFromNearestHeading()
    {
        using var docx = CreateDocxWithHeadings();
        var doc = docx.Document;

        var results = new List<ValidationResult>
        {
            new()
            {
                RuleName = "TestRule",
                Message = "Error in body after Chapter 1",
                IsError = true,
                Location = new DocumentLocation { Paragraph = 2, Text = "body text 1" }
            },
            new()
            {
                RuleName = "TestRule",
                Message = "Error in body after Section 1.1",
                IsError = true,
                Location = new DocumentLocation { Paragraph = 4, Text = "body text 2" }
            }
        };

        var headings = ThesisValidatorService.ExtractHeadings(doc);
        var (elementsMap, descendantsMap) = BuildSectionMaps(doc);
        PopulateSectionContext(results, elementsMap, descendantsMap);

        Assert.True(elementsMap.Count > 0, "elementsMap should not be empty.");
        Assert.True(descendantsMap.Count > 0, "descendantsMap should not be empty.");
        Assert.Equal("Chapter 1", results[0].Location.Section);
        Assert.Equal("Section 1.1", results[1].Location.Section);
    }

    [Fact]
    public void PopulateSectionContext_NoHeadings_LeavesBlank()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Just a paragraph", null),
            ("Another paragraph", null)
        );

        var results = new List<ValidationResult>
        {
            new()
            {
                RuleName = "TestRule",
                Message = "Error",
                IsError = true,
                Location = new DocumentLocation { Paragraph = 1, Text = "test" }
            }
        };

        var (elementsMap, descendantsMap) = BuildSectionMaps(docx.Document);
        PopulateSectionContext(results, elementsMap, descendantsMap);

        Assert.Empty(results[0].Location.Section);
    }

    /// <summary>
    /// Verifies section context works when a table adds extra paragraphs
    /// between headings (Descendants count diverges from Elements count).
    /// </summary>
    [Fact]
    public void PopulateSectionContext_WithTable_ElementsBasedRuleGetsSection()
    {
        using var docx = CreateDocxWithHeadingsAndTable();
        var doc = docx.Document;

        // Simulate a rule using Elements<Paragraph>() — paragraph index 3 is body text after Heading1
        // In Elements counting: H1=1, BodyText=2, Table(skipped), H2=3, BodyAfterH2=4
        // In Descendants counting: H1=1, BodyText=2, TableP1=3, TableP2=4, H2=5, BodyAfterH2=6
        var results = new List<ValidationResult>
        {
            new()
            {
                RuleName = "FontFamily",
                Message = "Wrong font in body after H2",
                IsError = true,
                // Elements-based index: paragraph 4 (BodyAfterH2)
                Location = new DocumentLocation { Paragraph = 4, Text = "body after h2" }
            }
        };

        var (elementsMap, descendantsMap) = BuildSectionMaps(doc);
        PopulateSectionContext(results, elementsMap, descendantsMap);

        // Should find "Heading 2" as nearest section
        Assert.Equal("Heading 2", results[0].Location.Section);
    }

    [Fact]
    public void PopulateSectionContext_WithTable_DescendantsBasedRuleGetsSection()
    {
        using var docx = CreateDocxWithHeadingsAndTable();
        var doc = docx.Document;

        // Simulate a rule using Descendants<Paragraph>() — index 6 is body after H2
        var results = new List<ValidationResult>
        {
            new()
            {
                RuleName = "SingleSpaceRule",
                Message = "Double spaces found",
                IsError = true,
                // Descendants-based index: paragraph 6 (BodyAfterH2)
                Location = new DocumentLocation { Paragraph = 6, Text = "body after h2" }
            }
        };

        var (elementsMap, descendantsMap) = BuildSectionMaps(doc);
        PopulateSectionContext(results, elementsMap, descendantsMap);

        Assert.Equal("Heading 2", results[0].Location.Section);
    }

    // ── Replicate private methods from ThesisValidatorService ──

    private static (
        List<(int Index, string Text)> ElementsMap,
        List<(int Index, string Text)> DescendantsMap
    ) BuildSectionMaps(WordprocessingDocument doc)
    {
        var elementsMap = new List<(int, string)>();
        var descendantsMap = new List<(int, string)>();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null) return (elementsMap, descendantsMap);

        int elemIdx = 0;
        foreach (var para in body.Elements<Paragraph>())
        {
            elemIdx++;
            var level = HeadingStyleHelper.GetHeadingLevel(doc, para);
            if (level is null) continue;
            var text = string.Concat(para.Descendants<Text>().Select(t => t.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            elementsMap.Add((elemIdx, text));
        }

        int descIdx = 0;
        foreach (var para in body.Descendants<Paragraph>())
        {
            descIdx++;
            var level = HeadingStyleHelper.GetHeadingLevel(doc, para);
            if (level is null) continue;
            var text = string.Concat(para.Descendants<Text>().Select(t => t.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            descendantsMap.Add((descIdx, text));
        }

        return (elementsMap, descendantsMap);
    }

    private static readonly HashSet<string> ElementsBasedRules = new(StringComparer.OrdinalIgnoreCase)
    {
        "FontFamily", "ListConsistencyRule", "Grammar",
        "FigureCaptionStyleRule", "EmptySectionStructureRule",
        "HeadingStyleUsageRule", "CheckTableOfContents",
    };

    private static void PopulateSectionContext(
        List<ValidationResult> results,
        List<(int Index, string Text)> elementsMap,
        List<(int Index, string Text)> descendantsMap)
    {
        foreach (var result in results)
        {
            var paraIdx = result.Location?.Paragraph ?? 0;
            if (paraIdx <= 0) continue;

            var map = ElementsBasedRules.Contains(result.RuleName)
                ? elementsMap
                : descendantsMap;

            var section = FindNearestSection(map, paraIdx);

            if (section is not null)
                result.Location!.Section = section;
        }
    }

    private static string? FindNearestSection(List<(int Index, string Text)> map, int paraIdx)
    {
        string? nearest = null;
        foreach (var (idx, text) in map)
        {
            if (idx <= paraIdx)
                nearest = text;
            else
                break;
        }
        return nearest;
    }

    // ── Test document builders ──

    private static InMemoryDocx CreateDocxWithHeadings()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style(new StyleParagraphProperties(new OutlineLevel { Val = 0 }))
            { Type = StyleValues.Paragraph, StyleId = "Heading1", StyleName = new StyleName { Val = "heading 1" } },
            new Style(new StyleParagraphProperties(new OutlineLevel { Val = 1 }))
            { Type = StyleValues.Paragraph, StyleId = "Heading2", StyleName = new StyleName { Val = "heading 2" } }
        );

        var body = mainPart.Document.Body!;
        body.Append(new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }), new Run(new Text("Chapter 1"))));
        body.Append(new Paragraph(new Run(new Text("This is body text under chapter 1."))));
        body.Append(new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "Heading2" }), new Run(new Text("Section 1.1"))));
        body.Append(new Paragraph(new Run(new Text("This is body text under section 1.1."))));

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }

    /// <summary>
    /// Creates: Heading1, BodyText, Table(2 cell paragraphs), Heading2, BodyText
    /// Elements count: H1=1, Body=2, H2=3, Body=4
    /// Descendants count: H1=1, Body=2, TP1=3, TP2=4, H2=5, Body=6
    /// </summary>
    private static InMemoryDocx CreateDocxWithHeadingsAndTable()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style(new StyleParagraphProperties(new OutlineLevel { Val = 0 }))
            { Type = StyleValues.Paragraph, StyleId = "Heading1", StyleName = new StyleName { Val = "heading 1" } },
            new Style(new StyleParagraphProperties(new OutlineLevel { Val = 1 }))
            { Type = StyleValues.Paragraph, StyleId = "Heading2", StyleName = new StyleName { Val = "heading 2" } }
        );

        var body = mainPart.Document.Body!;

        // Heading 1
        body.Append(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
            new Run(new Text("Heading 1"))));

        // Body text
        body.Append(new Paragraph(new Run(new Text("Body text after heading 1."))));

        // Table with 2 cell paragraphs (these show up in Descendants but not Elements)
        var table = new Table(
            new TableRow(
                new TableCell(new Paragraph(new Run(new Text("Cell 1")))),
                new TableCell(new Paragraph(new Run(new Text("Cell 2"))))
            )
        );
        body.Append(table);

        // Heading 2
        body.Append(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading2" }),
            new Run(new Text("Heading 2"))));

        // Body text
        body.Append(new Paragraph(new Run(new Text("Body text after heading 2."))));

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }
}
