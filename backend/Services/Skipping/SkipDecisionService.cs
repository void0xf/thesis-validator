using backend.Services.Formatting;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Skipping;

public static class SkipDecisionService
{
    private static readonly ISkipRule[] ParagraphRules =
    [
        new TableOfContentsSkipRule(),
        new TextBoxSkipRule()
    ];

    private static readonly ISkipRule[] RunRules =
    [
        new TextBoxSkipRule()
    ];

    private static readonly StructuralStyleSkipRule StructuralStyleRule = new();

    public static bool ShouldSkipTextBoxes(UniversityConfig config)
    {
        return config.Analysis.SkipTextBoxes ?? config.Formatting.SkipTextBoxes;
    }

    public static bool ShouldSkipTableOfContentsContent(UniversityConfig config)
    {
        return config.Analysis.SkipTableOfContentsContent
            ?? config.Formatting.SkipTableOfContentsContent;
    }

    public static bool ShouldSkipBeforeTableOfContents(UniversityConfig config)
    {
        return config.Analysis.SkipBeforeTableOfContents
            ?? config.Formatting.SkipBeforeTableOfContents;
    }

    public static SkipDecision ShouldSkipParagraph(
        WordprocessingDocument doc,
        Paragraph paragraph,
        UniversityConfig config,
        SkipContext? context = null)
    {
        var effectiveContext = context ?? new SkipContext();
        if (effectiveContext.ParagraphIndex < effectiveContext.FirstIncludedParagraphIndex)
        {
            return SkipDecision.Skip(
                SkipReason.BeforeTableOfContents,
                "Content appears before the configured table-of-contents boundary.");
        }

        foreach (var rule in ParagraphRules)
        {
            var decision = rule.ShouldSkipParagraph(doc, paragraph, config, effectiveContext);
            if (decision.ShouldSkip)
                return decision;
        }

        return SkipDecision.Include;
    }

    public static SkipDecision ShouldSkipRun(
        WordprocessingDocument doc,
        Paragraph paragraph,
        Run run,
        UniversityConfig config,
        SkipContext? context = null)
    {
        var effectiveContext = context ?? new SkipContext();
        foreach (var rule in RunRules)
        {
            var decision = rule.ShouldSkipRun(doc, paragraph, run, config, effectiveContext);
            if (decision.ShouldSkip)
                return decision;
        }

        return SkipDecision.Include;
    }

    public static SkipDecision ShouldSkipElement(
        OpenXmlElement element,
        UniversityConfig config,
        SkipContext? context = null)
    {
        var effectiveContext = context ?? new SkipContext();
        foreach (var rule in ParagraphRules.Concat(RunRules))
        {
            var decision = rule.ShouldSkipElement(element, config, effectiveContext);
            if (decision.ShouldSkip)
                return decision;
        }

        return SkipDecision.Include;
    }

    public static SkipDecision ShouldSkipStructuralStyle(
        WordprocessingDocument doc,
        Paragraph paragraph,
        bool excludeListStyles = true)
    {
        return StructuralStyleRule.ShouldSkipParagraph(
            doc,
            paragraph,
            new UniversityConfig(),
            new SkipContext(),
            excludeListStyles);
    }

    public static SkipDecision ShouldSkipStructuralStyle(
        Paragraph paragraph,
        bool excludeListStyles = true)
    {
        return StructuralStyleRule.ShouldSkipParagraph(
            null,
            paragraph,
            new UniversityConfig(),
            new SkipContext(),
            excludeListStyles);
    }

    public static bool HasExcludedStructuralStyle(
        Paragraph paragraph,
        bool excludeListStyles = true)
    {
        return ShouldSkipStructuralStyle(paragraph, excludeListStyles).ShouldSkip;
    }

    public static bool HasExcludedStructuralStyle(
        WordprocessingDocument doc,
        Paragraph paragraph,
        bool excludeListStyles = true)
    {
        return ShouldSkipStructuralStyle(doc, paragraph, excludeListStyles).ShouldSkip;
    }

    public static bool IsListItem(Paragraph paragraph)
    {
        return paragraph.ParagraphProperties?.NumberingProperties is not null;
    }

    public static string? GetParagraphStyleId(Paragraph paragraph)
    {
        return StyleResolutionService.GetParagraphStyleId(paragraph);
    }
}
