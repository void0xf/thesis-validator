using backend.ModernServices.Structure;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.ModernServices;

public sealed class ModernFigureCaptionAnalyzer
{
    private const int MaxFollowingParagraphs = 4;
    private const int MaxPreviousParagraphs = 2;

    public IReadOnlyList<FigureCaptionAssociation> AssociateFiguresWithCaptions(
        RuleContext context,
        bool requireStructuredCaption = false)
    {
        var paragraphs = context.Content.BodyChildParagraphs;
        var associations = new List<FigureCaptionAssociation>();

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            if (!FigureDetectionService.ContainsFigureCandidate(paragraph.Paragraph))
                continue;

            var figure = new FigureCandidate(paragraph.Paragraph, paragraph.BodyIndex);
            var below = FindFollowingCaption(context, paragraphs, i, requireStructuredCaption);
            if (below is not null)
            {
                associations.Add(new FigureCaptionAssociation(figure, below.Value.Caption, below.Value.Relation));
                continue;
            }

            var above = FindPreviousCaption(context, paragraphs, i, requireStructuredCaption);
            associations.Add(above is null
                ? new FigureCaptionAssociation(figure, null, FigureCaptionRelationKind.None)
                : new FigureCaptionAssociation(figure, above, FigureCaptionRelationKind.Above));
        }

        return associations;
    }

    public IReadOnlyList<FigureCaptionCandidate> GetDetectedFigureCaptions(RuleContext context)
    {
        return AssociateFiguresWithCaptions(context)
            .Where(association => association.Caption is not null)
            .Select(association => association.Caption!)
            .GroupBy(caption => caption.ParagraphIndex)
            .Select(group => group.First())
            .ToList();
    }

    private static (FigureCaptionCandidate Caption, FigureCaptionRelationKind Relation)? FindFollowingCaption(
        RuleContext context,
        IReadOnlyList<ParagraphNode> paragraphs,
        int figureListIndex,
        bool requireStructuredCaption)
    {
        var hasInterveningContent = false;
        var end = Math.Min(paragraphs.Count - 1, figureListIndex + MaxFollowingParagraphs);

        for (var i = figureListIndex + 1; i <= end; i++)
        {
            var paragraph = paragraphs[i];
            var hasMeaningfulText = !string.IsNullOrWhiteSpace(paragraph.Text);
            var containsFigure = FigureDetectionService.ContainsFigureCandidate(paragraph.Paragraph);
            var caption = CreateCaptionCandidate(context, paragraph);

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
        RuleContext context,
        IReadOnlyList<ParagraphNode> paragraphs,
        int figureListIndex,
        bool requireStructuredCaption)
    {
        var start = Math.Max(0, figureListIndex - MaxPreviousParagraphs);

        for (var i = figureListIndex - 1; i >= start; i--)
        {
            var paragraph = paragraphs[i];
            var hasMeaningfulText = !string.IsNullOrWhiteSpace(paragraph.Text);
            var containsFigure = FigureDetectionService.ContainsFigureCandidate(paragraph.Paragraph);
            var caption = CreateCaptionCandidate(context, paragraph);

            if (caption is not null && (!requireStructuredCaption || caption.IsStructuredCaption))
                return caption;

            if (!hasMeaningfulText && !containsFigure)
                continue;

            break;
        }

        return null;
    }

    private static FigureCaptionCandidate? CreateCaptionCandidate(
        RuleContext context,
        ParagraphNode paragraph)
    {
        var text = paragraph.Text.Trim();
        var hasMeaningfulText = !string.IsNullOrWhiteSpace(text);
        var hasFigureSequenceField = CaptionDetectionService.HasFigureSequenceField(paragraph.Paragraph);
        var usesDedicatedCaptionStyle = hasMeaningfulText
            && CaptionDetectionService.UsesDedicatedCaptionStyle(context.RawDocument, paragraph.Paragraph);
        var looksLikeFigureCaptionText = hasMeaningfulText
            && CaptionDetectionService.LooksLikeFigureCaptionText(text);

        return hasFigureSequenceField || usesDedicatedCaptionStyle || looksLikeFigureCaptionText
            ? new FigureCaptionCandidate(
                paragraph.Paragraph,
                paragraph.BodyIndex,
                text,
                hasFigureSequenceField,
                usesDedicatedCaptionStyle,
                looksLikeFigureCaptionText)
            : null;
    }
}

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
