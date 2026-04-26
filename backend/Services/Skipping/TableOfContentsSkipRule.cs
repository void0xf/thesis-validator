using Backend.Models;
using backend.Services.Structure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Skipping;

public sealed class TableOfContentsSkipRule : ISkipRule
{
    public SkipDecision ShouldSkipParagraph(
        WordprocessingDocument doc,
        Paragraph paragraph,
        UniversityConfig config,
        SkipContext context)
    {
        if (context.TableOfContentsParagraphs?.Contains(paragraph) == true)
        {
            return SkipDecision.Skip(
                SkipReason.TableOfContents,
                "Paragraph belongs to table-of-contents content.");
        }

        return SkipDecision.Include;
    }

    public static bool IsTableOfContentsParagraph(Paragraph paragraph)
    {
        return TableOfContentsDetectionService.ContainsTocFieldCode(paragraph)
            || IsTocStyleParagraph(paragraph);
    }

    public static HashSet<Paragraph> GetSkippedParagraphs(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        var paragraphs = new HashSet<Paragraph>();
        if (!SkipDecisionService.ShouldSkipTableOfContentsContent(config))
            return paragraphs;

        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return paragraphs;

        bool inTocField = false;
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

    public static int GetFirstIncludedDescendantParagraphIndex(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        if (!SkipDecisionService.ShouldSkipBeforeTableOfContents(config))
            return 1;

        var detection = TableOfContentsDetectionService.Detect(doc);
        return detection.ParagraphIndex > 0 ? detection.ParagraphIndex + 1 : 1;
    }

    public static int GetFirstIncludedBodyParagraphIndex(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        if (!SkipDecisionService.ShouldSkipBeforeTableOfContents(config))
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

    public static int GetFirstIncludedBodyChildIndex(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        if (!SkipDecisionService.ShouldSkipBeforeTableOfContents(config))
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

        int childIndex = 0;
        foreach (var child in body.ChildElements)
        {
            if (ReferenceEquals(child, tocBodyChild))
                return childIndex + 1;

            childIndex++;
        }

        return 0;
    }

    public static bool IsTocStyleParagraph(Paragraph paragraph)
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
