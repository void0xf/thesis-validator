using backend.Services.Extraction;
using backend.Services.Skipping;
using backend.Services.Structure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;

namespace backend.ModernServices;

public sealed class ModernDocumentSkipService
{
    private readonly ModernValidationOptions _options;

    public ModernDocumentSkipService(IOptions<ModernValidationOptions> options)
    {
        _options = options.Value;
    }

    public bool SkipTextBoxes => _options.SkipTextBoxes;

    public int GetFirstIncludedBodyChildIndex(
        WordprocessingDocument doc)
    {
        if (!_options.SkipBeforeTableOfContents)
            return 0;

        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return 0;

        var tocParagraph = TableOfContentsDetectionService.Detect(doc).Paragraph;
        if (tocParagraph is null)
            return 0;

        var tocBodyChild = GetBodyChildAncestor(tocParagraph, body);
        if (tocBodyChild is null)
            return 0;

        var childIndex = 0;
        foreach (var child in body.ChildElements)
        {
            if (ReferenceEquals(child, tocBodyChild))
                return childIndex + 1;

            childIndex++;
        }

        return 0;
    }

    public HashSet<Paragraph> GetSkippedTableOfContentsParagraphs(
        WordprocessingDocument doc)
    {
        var paragraphs = new HashSet<Paragraph>();
        if (!_options.SkipTableOfContentsContent)
            return paragraphs;

        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return paragraphs;

        var inTocField = false;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            if (TableOfContentsDetectionService.ContainsTocFieldCode(paragraph))
            {
                inTocField = true;
                paragraphs.Add(paragraph);
            }

            if (inTocField)
            {
                paragraphs.Add(paragraph);
            }
            else if (IsTocStyleParagraph(paragraph))
            {
                paragraphs.Add(paragraph);
            }

            if (inTocField && ContainsFieldEnd(paragraph))
            {
                inTocField = false;
            }
        }

        return paragraphs;
    }

    public bool ShouldSkipParagraph(
        Paragraph paragraph,
        IReadOnlySet<Paragraph> tableOfContentsParagraphs)
    {
        if (tableOfContentsParagraphs.Contains(paragraph))
            return true;

        return _options.SkipTextBoxes
            && (TextBoxSkipRule.IsInsideTextBoxOrDrawingText(paragraph)
                || IsTextBoxOnlyParagraph(paragraph));
    }

    private static bool IsTextBoxOnlyParagraph(Paragraph paragraph)
    {
        return paragraph.Descendants<Text>().Any(TextBoxSkipRule.IsInsideTextBoxOrDrawingText)
            && !TextExtractionService.HasMeaningfulContent(GetParagraphTextWithoutTextBoxes(paragraph));
    }

    private static string GetParagraphTextWithoutTextBoxes(Paragraph paragraph)
    {
        return string.Concat(paragraph
            .Descendants<Text>()
            .Where(text => !TextBoxSkipRule.IsInsideTextBoxOrDrawingText(text))
            .Select(text => text.Text));
    }

    private static bool IsTocStyleParagraph(Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrWhiteSpace(styleId))
            return false;

        var normalized = styleId.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        return normalized.StartsWith("TOC", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Spis", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("TableOfContents", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsFieldEnd(Paragraph paragraph)
    {
        return paragraph.Descendants<FieldChar>()
            .Any(fieldChar => fieldChar.FieldCharType?.Value == FieldCharValues.End);
    }

    private static OpenXmlElement? GetBodyChildAncestor(OpenXmlElement element, Body body)
    {
        OpenXmlElement? current = element;
        while (current?.Parent is not null && current.Parent != body)
        {
            current = current.Parent;
        }

        return current?.Parent == body ? current : null;
    }
}
