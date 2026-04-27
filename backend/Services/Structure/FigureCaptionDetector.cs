using backend.Services.Analysis;
using backend.Services.Extraction;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Structure;

public enum FigureCaptionRelationKind
{
    None,
    Below,
    Above,
    SeparatedBelow
}

public sealed record FigureCandidate(Paragraph Paragraph, int ParagraphIndex);

public sealed record FigureCaptionCandidate(
    Paragraph Paragraph,
    int ParagraphIndex,
    string Text,
    bool HasFigureSequenceField,
    bool UsesDedicatedCaptionStyle,
    bool LooksLikeFigureCaptionText)
{
    public bool IsStructuredCaption => HasFigureSequenceField || UsesDedicatedCaptionStyle;
}

public sealed record FigureCaptionAssociation(
    FigureCandidate Figure,
    FigureCaptionCandidate? Caption,
    FigureCaptionRelationKind Relation)
{
    public bool HasCaption => Caption is not null;
    public bool IsCaptionBelowFigure => Relation == FigureCaptionRelationKind.Below;
}

public static class FigureCaptionDetector
{
    private const int MaxFollowingParagraphs = 4;
    private const int MaxPreviousParagraphs = 2;

    public static IReadOnlyList<FigureCaptionAssociation> AssociateFiguresWithCaptions(
        WordprocessingDocument doc,
        UniversityConfig config,
        bool requireStructuredCaption = false)
    {
        var paragraphs = DocumentAnalysisScope.DescendantParagraphs(doc, config).ToList();
        var associations = new List<FigureCaptionAssociation>();

        for (int i = 0; i < paragraphs.Count; i++)
        {
            var (paragraph, paragraphIndex) = paragraphs[i];
            if (!FigureDetectionService.ContainsFigureCandidate(paragraph, config))
                continue;

            var figure = new FigureCandidate(paragraph, paragraphIndex);
            var below = FindFollowingCaption(doc, config, paragraphs, i, requireStructuredCaption);
            if (below is not null)
            {
                associations.Add(new FigureCaptionAssociation(figure, below.Value.Caption, below.Value.Relation));
                continue;
            }

            var above = FindPreviousCaption(doc, config, paragraphs, i, requireStructuredCaption);
            associations.Add(above is null
                ? new FigureCaptionAssociation(figure, null, FigureCaptionRelationKind.None)
                : new FigureCaptionAssociation(figure, above, FigureCaptionRelationKind.Above));
        }

        return associations;
    }

    public static IReadOnlyList<FigureCaptionCandidate> GetDetectedFigureCaptions(
        WordprocessingDocument doc,
        UniversityConfig config)
    {
        return AssociateFiguresWithCaptions(doc, config)
            .Where(association => association.Caption is not null)
            .Select(association => association.Caption!)
            .GroupBy(caption => caption.ParagraphIndex)
            .Select(group => group.First())
            .ToList();
    }

    private static (FigureCaptionCandidate Caption, FigureCaptionRelationKind Relation)? FindFollowingCaption(
        WordprocessingDocument doc,
        UniversityConfig config,
        IReadOnlyList<(Paragraph Paragraph, int Index)> paragraphs,
        int figureListIndex,
        bool requireStructuredCaption)
    {
        var hasInterveningContent = false;
        var end = Math.Min(paragraphs.Count - 1, figureListIndex + MaxFollowingParagraphs);

        for (int i = figureListIndex + 1; i <= end; i++)
        {
            var (paragraph, paragraphIndex) = paragraphs[i];
            var text = TextExtractionService.GetParagraphText(doc, paragraph, config).Trim();
            var hasMeaningfulText = TextExtractionService.HasMeaningfulContent(text);
            var containsFigure = FigureDetectionService.ContainsFigureCandidate(paragraph, config);
            var caption = CreateCaptionCandidate(doc, config, paragraph, paragraphIndex, text);

            if (caption is not null && (!requireStructuredCaption || caption.IsStructuredCaption))
            {
                return (
                    caption,
                    hasInterveningContent
                        ? FigureCaptionRelationKind.SeparatedBelow
                        : FigureCaptionRelationKind.Below);
            }

            if (containsFigure)
                break;

            if (!hasMeaningfulText)
                continue;

            hasInterveningContent = true;
        }

        return null;
    }

    private static FigureCaptionCandidate? FindPreviousCaption(
        WordprocessingDocument doc,
        UniversityConfig config,
        IReadOnlyList<(Paragraph Paragraph, int Index)> paragraphs,
        int figureListIndex,
        bool requireStructuredCaption)
    {
        var start = Math.Max(0, figureListIndex - MaxPreviousParagraphs);

        for (int i = figureListIndex - 1; i >= start; i--)
        {
            var (paragraph, paragraphIndex) = paragraphs[i];
            var text = TextExtractionService.GetParagraphText(doc, paragraph, config).Trim();
            var hasMeaningfulText = TextExtractionService.HasMeaningfulContent(text);
            var containsFigure = FigureDetectionService.ContainsFigureCandidate(paragraph, config);
            var caption = CreateCaptionCandidate(doc, config, paragraph, paragraphIndex, text);

            if (caption is not null && (!requireStructuredCaption || caption.IsStructuredCaption))
                return caption;

            if (!hasMeaningfulText && !containsFigure)
                continue;

            break;
        }

        return null;
    }

    private static FigureCaptionCandidate? CreateCaptionCandidate(
        WordprocessingDocument doc,
        UniversityConfig config,
        Paragraph paragraph,
        int paragraphIndex,
        string text)
    {
        var hasMeaningfulText = TextExtractionService.HasMeaningfulContent(text);
        var hasFigureSequenceField = CaptionDetectionService.HasFigureSequenceField(paragraph);
        var usesDedicatedCaptionStyle = hasMeaningfulText
            && CaptionDetectionService.UsesDedicatedCaptionStyle(doc, paragraph);
        var looksLikeFigureCaptionText = hasMeaningfulText
            && CaptionDetectionService.LooksLikeFigureCaptionText(text);

        return hasFigureSequenceField || usesDedicatedCaptionStyle || looksLikeFigureCaptionText
            ? new FigureCaptionCandidate(
                paragraph,
                paragraphIndex,
                text,
                hasFigureSequenceField,
                usesDedicatedCaptionStyle,
                looksLikeFigureCaptionText)
            : null;
    }
}
