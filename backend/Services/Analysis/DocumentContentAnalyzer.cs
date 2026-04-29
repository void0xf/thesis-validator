using Backend.Models;
using backend.Services.Extraction;
using backend.Services.Skipping;
using backend.Services.Structure;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Services.Analysis;

public sealed class DocumentContentAnalyzer
{
    public DocumentContent Analyze(WordprocessingDocument doc, UniversityConfig config)
    {
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return new DocumentContent();

        var paragraphs = new List<ParagraphNode>();
        var rootSections = new List<SectionNode>();
        var sectionStack = new List<SectionNode>();

        var firstIncludedChildIndex = DocumentAnalysisScope.GetFirstIncludedBodyChildIndex(doc, config);
        var tocParagraphs = TableOfContentsSkipRule.GetSkippedParagraphs(doc, config);

        int paragraphIndex = 0;
        int childIndex = 0;

        foreach (var element in body.ChildElements)
        {
            if (element is Paragraph)
                paragraphIndex++;

            if (childIndex++ < firstIncludedChildIndex)
                continue;

            if (element is not Paragraph paragraph)
            {
                MarkIntroductoryContent(sectionStack);
                continue;
            }

            var skipDecision = SkipDecisionService.ShouldSkipParagraph(
                doc,
                paragraph,
                config,
                new SkipContext(paragraphIndex, null, tocParagraphs));
            if (skipDecision.ShouldSkip)
                continue;

            var text = TextExtractionService.GetParagraphText(doc, paragraph, config).Trim();
            var headingLevel = HeadingDetectionService.GetHeadingLevel(doc, paragraph);
            var paragraphNode = new ParagraphNode
            {
                Paragraph = paragraph,
                BodyIndex = paragraphIndex,
                Text = text,
                HeadingLevel = headingLevel
            };
            paragraphs.Add(paragraphNode);

            if (headingLevel is not null)
            {
                AddSection(paragraphNode, rootSections, sectionStack);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
                MarkIntroductoryContent(sectionStack);
        }

        return new DocumentContent
        {
            BodyChildParagraphs = paragraphs,
            Sections = rootSections
        };
    }

    private static void AddSection(
        ParagraphNode heading,
        List<SectionNode> rootSections,
        List<SectionNode> sectionStack)
    {
        var level = heading.HeadingLevel!.Value;
        while (sectionStack.Count > 0 && sectionStack[^1].Level >= level)
        {
            sectionStack.RemoveAt(sectionStack.Count - 1);
        }

        var section = new SectionNode { Heading = heading };
        if (sectionStack.Count == 0)
        {
            rootSections.Add(section);
        }
        else
        {
            sectionStack[^1].Children.Add(section);
        }

        sectionStack.Add(section);
    }

    private static void MarkIntroductoryContent(List<SectionNode> sectionStack)
    {
        if (sectionStack.Count == 0)
            return;

        var currentSection = sectionStack[^1];
        if (currentSection.Children.Count == 0)
            currentSection.HasIntroductoryContent = true;
    }
}
