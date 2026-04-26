using Backend.Models;
using backend.Services.Extraction;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using backend.Services.Skipping;

namespace backend.Services.Analysis;

public enum DocumentBlockType
{
    BodyParagraph,
    TextBox,
    TableOfContents
}

public static class DocumentAnalysisScope
{
    public static IEnumerable<(Paragraph Paragraph, int Index)> BodyParagraphs(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            yield break;

        var firstIncludedIndex = TableOfContentsSkipRule.GetFirstIncludedBodyParagraphIndex(doc, config);
        var tocParagraphs = TableOfContentsSkipRule.GetSkippedParagraphs(doc, config);
        int paragraphIndex = 0;

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;
            var skipDecision = SkipDecisionService.ShouldSkipParagraph(
                doc,
                paragraph,
                config,
                new SkipContext(paragraphIndex, firstIncludedIndex, tocParagraphs));
            if (skipDecision.ShouldSkip)
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

        var firstIncludedIndex = TableOfContentsSkipRule.GetFirstIncludedDescendantParagraphIndex(doc, config);
        var tocParagraphs = includeTableOfContentsContent
            ? new HashSet<Paragraph>()
            : TableOfContentsSkipRule.GetSkippedParagraphs(doc, config);
        int paragraphIndex = 0;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            var skipDecision = SkipDecisionService.ShouldSkipParagraph(
                doc,
                paragraph,
                config,
                new SkipContext(paragraphIndex, firstIncludedIndex, tocParagraphs));
            if (skipDecision.ShouldSkip)
                continue;

            yield return (paragraph, paragraphIndex);
        }
    }

    public static DocumentBlockType GetBlockType(Paragraph paragraph)
    {
        if (TableOfContentsSkipRule.IsTableOfContentsParagraph(paragraph))
            return DocumentBlockType.TableOfContents;

        if (TextBoxSkipRule.IsInsideTextBoxOrDrawingText(paragraph)
            || TextBoxSkipRule.ContainsTextBoxContent(paragraph))
        {
            return DocumentBlockType.TextBox;
        }

        return DocumentBlockType.BodyParagraph;
    }

    public static string GetParagraphText(Paragraph paragraph, UniversityConfig config)
    {
        return TextExtractionService.GetParagraphText(paragraph, config);
    }

    public static bool HasMeaningfulParagraphContent(Paragraph paragraph, UniversityConfig config)
    {
        return TextExtractionService.HasMeaningfulParagraphContent(paragraph, config);
    }

    public static bool HasMeaningfulContent(string? text)
    {
        return TextExtractionService.HasMeaningfulContent(text);
    }

    public static string GetRunText(Run run, UniversityConfig config)
    {
        return TextExtractionService.GetRunText(run, config);
    }

    public static bool ContainsTextBoxContent(OpenXmlElement element)
    {
        return TextBoxSkipRule.ContainsTextBoxContent(element);
    }

    public static bool IsTableOfContentsParagraph(Paragraph paragraph)
    {
        return TableOfContentsSkipRule.IsTableOfContentsParagraph(paragraph);
    }

    public static int GetFirstIncludedBodyChildIndex(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        return TableOfContentsSkipRule.GetFirstIncludedBodyChildIndex(doc, config);
    }
}
