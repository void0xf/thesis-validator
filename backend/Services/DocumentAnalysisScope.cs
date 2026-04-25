using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;
using System.Globalization;

namespace backend.Services;

public enum DocumentBlockType
{
    BodyParagraph,
    TextBox,
    TableOfContents
}

public static class DocumentAnalysisScope
{
    private const string WordprocessingNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string DrawingNamespace = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private const string VmlNamespace = "urn:schemas-microsoft-com:vml";

    public static IEnumerable<(Paragraph Paragraph, int Index)> BodyParagraphs(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            yield break;

        var firstIncludedIndex = GetFirstIncludedBodyParagraphIndex(doc, config);
        var tocParagraphs = GetSkippedTableOfContentsParagraphs(doc, config);
        int paragraphIndex = 0;

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;
            if (paragraphIndex < firstIncludedIndex)
                continue;

            if (tocParagraphs.Contains(paragraph))
                continue;

            if (ShouldSkipTextBoxParagraph(paragraph, config))
                continue;

            yield return (paragraph, paragraphIndex);
        }
    }

    public static IEnumerable<(Paragraph Paragraph, int Index)> DescendantParagraphs(
        WordprocessingDocument doc,
        UniversityConfig config,
        bool includeTableOfContentsContent = false)
    {
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            yield break;

        var firstIncludedIndex = GetFirstIncludedDescendantParagraphIndex(doc, config);
        var tocParagraphs = includeTableOfContentsContent
            ? new HashSet<Paragraph>()
            : GetSkippedTableOfContentsParagraphs(doc, config);
        int paragraphIndex = 0;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            if (paragraphIndex < firstIncludedIndex)
                continue;

            if (tocParagraphs.Contains(paragraph))
                continue;

            if (ShouldSkipTextBoxParagraph(paragraph, config))
                continue;

            yield return (paragraph, paragraphIndex);
        }
    }

    public static DocumentBlockType GetBlockType(Paragraph paragraph)
    {
        if (IsTocStyleParagraph(paragraph))
            return DocumentBlockType.TableOfContents;

        if (IsTextBoxParagraph(paragraph) || IsTextBoxOnlyParagraph(paragraph))
            return DocumentBlockType.TextBox;

        return DocumentBlockType.BodyParagraph;
    }

    public static string GetParagraphText(Paragraph paragraph, UniversityConfig config)
    {
        var textElements = paragraph.Descendants<Text>();
        if (config.Formatting.SkipTextBoxes)
        {
            textElements = textElements.Where(text => !IsInsideTextBoxOrDrawingText(text));
        }

        return string.Concat(textElements.Select(t => t.Text));
    }

    public static bool HasMeaningfulParagraphContent(Paragraph paragraph, UniversityConfig config)
    {
        var text = GetParagraphText(paragraph, config);
        return HasMeaningfulContent(text);
    }

    public static bool HasMeaningfulContent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
                continue;

            if (char.GetUnicodeCategory(ch) == UnicodeCategory.Format)
                continue;

            return true;
        }

        return false;
    }

    public static string GetRunText(Run run, UniversityConfig config)
    {
        var textElements = run.Elements<Text>();
        if (config.Formatting.SkipTextBoxes)
        {
            textElements = textElements.Where(text => !IsInsideTextBoxOrDrawingText(text));
        }

        return string.Concat(textElements.Select(t => t.Text));
    }

    public static bool ContainsTextBoxContent(OpenXmlElement element)
    {
        return ContainsElement(element, IsTextBoxContentElement);
    }

    public static bool IsTableOfContentsParagraph(Paragraph paragraph)
    {
        return ContainsTocFieldCode(paragraph) || IsTocStyleParagraph(paragraph);
    }

    public static int GetFirstIncludedBodyChildIndex(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        if (!config.Formatting.SkipBeforeTableOfContents)
            return 0;

        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return 0;

        var tocParagraph = TocRule.DetectTableOfContents(doc).Paragraph;
        if (tocParagraph is null)
            return 0;

        var tocBodyChild = GetBodyChildAncestor(tocParagraph, body);
        if (tocBodyChild is null)
            return 0;

        int childIndex = 0;
        foreach (var child in body.ChildElements)
        {
            if (ReferenceEquals(child, tocBodyChild))
                return childIndex + 1;

            childIndex++;
        }

        return 0;
    }

    private static int GetFirstIncludedDescendantParagraphIndex(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        if (!config.Formatting.SkipBeforeTableOfContents)
            return 1;

        var detection = TocRule.DetectTableOfContents(doc);
        return detection.ParagraphIndex > 0 ? detection.ParagraphIndex + 1 : 1;
    }

    private static int GetFirstIncludedBodyParagraphIndex(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        if (!config.Formatting.SkipBeforeTableOfContents)
            return 1;

        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return 1;

        var firstIncludedChildIndex = GetFirstIncludedBodyChildIndex(doc, config);
        int paragraphIndex = 0;
        int childIndex = 0;

        foreach (var child in body.ChildElements)
        {
            if (child is Paragraph)
                paragraphIndex++;

            if (childIndex >= firstIncludedChildIndex && child is Paragraph)
                return paragraphIndex;

            childIndex++;
        }

        return int.MaxValue;
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

    private static HashSet<Paragraph> GetSkippedTableOfContentsParagraphs(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        var paragraphs = new HashSet<Paragraph>();
        if (!config.Formatting.SkipTableOfContentsContent)
            return paragraphs;

        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return paragraphs;

        bool inTocField = false;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            if (ContainsTocFieldCode(paragraph))
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

    private static bool ContainsTocFieldCode(Paragraph paragraph)
    {
        return paragraph.Descendants<FieldCode>()
            .Any(fieldCode => fieldCode.Text.Trim().StartsWith("TOC", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsFieldEnd(Paragraph paragraph)
    {
        return paragraph.Descendants<FieldChar>()
            .Any(fieldChar => fieldChar.FieldCharType?.Value == FieldCharValues.End);
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

    private static bool ShouldSkipTextBoxParagraph(Paragraph paragraph, UniversityConfig config)
    {
        return config.Formatting.SkipTextBoxes
            && (IsTextBoxParagraph(paragraph) || IsTextBoxOnlyParagraph(paragraph));
    }

    private static bool IsTextBoxParagraph(Paragraph paragraph)
    {
        return IsInsideTextBoxOrDrawingText(paragraph);
    }

    private static bool IsTextBoxOnlyParagraph(Paragraph paragraph)
    {
        return paragraph.Descendants<Text>().Any(IsInsideTextBoxOrDrawingText)
            && !HasMeaningfulContent(GetParagraphTextWithoutTextBoxes(paragraph));
    }

    private static string GetParagraphTextWithoutTextBoxes(Paragraph paragraph)
    {
        return string.Concat(paragraph
            .Descendants<Text>()
            .Where(text => !IsInsideTextBoxOrDrawingText(text))
            .Select(text => text.Text));
    }

    private static bool IsInsideTextBoxOrDrawingText(OpenXmlElement element)
    {
        OpenXmlElement? current = element;
        while (current is not null)
        {
            if (IsTextBoxOrDrawingTextElement(current))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private static bool IsTextBoxOrDrawingTextElement(OpenXmlElement element)
    {
        return IsWordprocessingElement(element, "drawing")
            || IsWordprocessingElement(element, "pict")
            || IsWordprocessingElement(element, "txbxContent")
            || IsVmlElement(element, "shape")
            || IsVmlElement(element, "textbox")
            || IsDrawingElement(element, "txBody");
    }

    private static bool IsTextBoxContentElement(OpenXmlElement element)
    {
        return IsWordprocessingElement(element, "txbxContent")
            || IsVmlElement(element, "textbox")
            || IsDrawingElement(element, "txBody");
    }

    private static bool ContainsElement(OpenXmlElement element, Func<OpenXmlElement, bool> predicate)
    {
        if (predicate(element))
            return true;

        foreach (var child in element.ChildElements)
        {
            if (ContainsElement(child, predicate))
                return true;
        }

        return false;
    }

    private static bool IsWordprocessingElement(OpenXmlElement element, string localName)
    {
        return element.LocalName == localName && element.NamespaceUri == WordprocessingNamespace;
    }

    private static bool IsDrawingElement(OpenXmlElement element, string localName)
    {
        return element.LocalName == localName && element.NamespaceUri == DrawingNamespace;
    }

    private static bool IsVmlElement(OpenXmlElement element, string localName)
    {
        return element.LocalName == localName && element.NamespaceUri == VmlNamespace;
    }
}
