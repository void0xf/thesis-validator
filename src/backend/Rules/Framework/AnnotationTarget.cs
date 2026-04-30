using DocumentFormat.OpenXml.Wordprocessing;

namespace ThesisValidator.Rules;

public abstract record AnnotationTarget;

public sealed record ParagraphAnnotationTarget(Paragraph Paragraph) : AnnotationTarget;

public sealed record RunAnnotationTarget(Run Run) : AnnotationTarget;
