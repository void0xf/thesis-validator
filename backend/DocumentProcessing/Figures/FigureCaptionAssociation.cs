using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.Figures;

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
