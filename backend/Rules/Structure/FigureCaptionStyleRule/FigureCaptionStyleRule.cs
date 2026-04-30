using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.Models;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Figures;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates the Word style and paragraph formatting of existing figure captions.
/// </summary>
public sealed class FigureCaptionStyleRule : ValidationRule<FigureCaptionStyleRuleOptions>
{
    public const string RuleId = nameof(FigureCaptionStyleRule);

    private const double ExpectedFontSizePt = 11.0;
    private const int IndentToleranceTwips = 10;

    private readonly FormattingResolver _formattingResolver = new();
    private readonly FigureCaptionAnalyzer _figureCaptionAnalyzer = new();

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Figure Caption Style",
        Description: "Finds figure captions that do not use the expected style, alignment, font size, or indentation.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        FigureCaptionStyleRuleOptions options)
    {
        foreach (var captionCandidate in _figureCaptionAnalyzer.GetDetectedFigureCaptions(context))
        {
            var caption = captionCandidate.Paragraph;
            var preview = TextExtractor.Truncate(captionCandidate.Text, 50);

            if (!CaptionDetection.UsesDedicatedCaptionStyle(context.RawDocument, caption))
            {
                var label = CaptionDetection.GetCaptionStyleLabel(context.RawDocument, caption);
                yield return new RuleProblem(
                    $"Figure caption uses \"{label}\" style - assign a Caption style (e.g., \"Caption\", \"Legenda\").",
                    new DocumentLocation
                    {
                        Paragraph = captionCandidate.ParagraphIndex,
                        Text = preview
                    },
                    ParagraphIndexKind.BodyElement,
                    new ParagraphAnnotationTarget(captionCandidate.Paragraph));
            }

            var textRun = caption.Elements<Run>()
                .FirstOrDefault(run => !string.IsNullOrWhiteSpace(TextExtractor.GetRunText(run)));
            if (textRun is not null)
            {
                var pt = _formattingResolver.ResolveFontSizePt(
                    context.RawDocument,
                    caption,
                    textRun);
                if (pt is not null && Math.Abs(pt.Value - ExpectedFontSizePt) > 0.01)
                {
                    yield return new RuleProblem(
                        $"Figure caption font size must be 11pt, found {pt:0.##}pt.",
                        new DocumentLocation
                        {
                            Paragraph = captionCandidate.ParagraphIndex,
                            Text = preview
                        },
                        ParagraphIndexKind.BodyElement,
                        new ParagraphAnnotationTarget(captionCandidate.Paragraph));
                }
            }

            var justification = _formattingResolver.ResolveJustification(
                context.RawDocument,
                caption,
                includeDefaultStyle: false);
            if (justification != JustificationValues.Center)
            {
                var name = justification == JustificationValues.Right
                    ? "right-aligned"
                    : justification == JustificationValues.Both
                        ? "justified"
                        : "left-aligned";

                yield return new RuleProblem(
                    $"Figure caption must be centered, found {name}.",
                    new DocumentLocation
                    {
                        Paragraph = captionCandidate.ParagraphIndex,
                        Text = preview
                    },
                    ParagraphIndexKind.BodyElement,
                    new ParagraphAnnotationTarget(captionCandidate.Paragraph));
            }

            var (left, firstLine) = _formattingResolver.ResolveIndentation(
                context.RawDocument,
                caption,
                includeDefaultStyle: false);
            if (Math.Abs(left) > IndentToleranceTwips || Math.Abs(firstLine) > IndentToleranceTwips)
            {
                yield return new RuleProblem(
                    $"Figure caption must have no indentation (left: {left / UnitConversion.TwipsPerCm:F2}cm, first-line: {firstLine / UnitConversion.TwipsPerCm:F2}cm).",
                    new DocumentLocation
                    {
                        Paragraph = captionCandidate.ParagraphIndex,
                        Text = preview
                    },
                    ParagraphIndexKind.BodyElement,
                    new ParagraphAnnotationTarget(captionCandidate.Paragraph));
            }
        }
    }
}
