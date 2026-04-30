using backend.Models;
using backend.ModernServices;
using backend.RuleOptions;
using backend.ModernServices.Extraction;
using backend.ModernServices.Formatting;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class ParagraphIndentRule : ValidationRule<ParagraphIndentRuleOptions>
{
    public const string RuleId = "RequiredIndentCm";

    private readonly ModernFormattingResolver _formattingResolver;
    private readonly ModernParagraphClassifier _paragraphClassifier;

    public ParagraphIndentRule(
        ModernFormattingResolver? formattingResolver = null,
        ModernParagraphClassifier? paragraphClassifier = null)
    {
        _formattingResolver = formattingResolver ?? new ModernFormattingResolver();
        _paragraphClassifier = paragraphClassifier ?? new ModernParagraphClassifier();
    }

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Paragraph Indent",
        Description: "Finds body paragraphs with an incorrect first-line indent.",
        Category: RuleCategories.Layout,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        ParagraphIndentRuleOptions options)
    {
        var allowedIndentsTwips = options.AllowedIndentTwips ?? [];

        foreach (var paragraph in context.Content.BodyChildParagraphs)
        {
            if (paragraph.IsHeading)
                continue;

            if (_paragraphClassifier.HasExcludedStructuralStyle(context.RawDocument, paragraph.Paragraph))
                continue;

            if (!TextExtractionService.HasMeaningfulContent(paragraph.Text))
                continue;

            if (IsCenteredOrRightAligned(context, paragraph.Paragraph))
                continue;

            if (_paragraphClassifier.IsListItem(paragraph.Paragraph))
                continue;

            var preview = TextExtractionService.Truncate(paragraph.Text, 50);
            if (StartsWithTabCharacter(paragraph.Paragraph))
            {
                var message =
                    $"Paragraph uses TAB character for indent instead of proper first-line indent formatting. Please use paragraph formatting ({FormatAllowedIndents(options)} first-line indent) instead of TAB.";

                yield return CreateProblem(message, paragraph, preview);
                continue;
            }

            var firstLineIndent = _formattingResolver.ResolveFirstLineIndent(
                context.RawDocument,
                paragraph.Paragraph);

            if (IsValidIndent(firstLineIndent, allowedIndentsTwips, options.ToleranceTwips))
                continue;

            var actualIndentCm = firstLineIndent / UnitConversion.TwipsPerCm;
            var indentMessage =
                $"Paragraph has incorrect first line indent: {actualIndentCm:F2} cm. Expected {FormatAllowedIndents(options)}.";

            yield return CreateProblem(indentMessage, paragraph, preview);
        }
    }

    private static RuleProblem CreateProblem(
        string message,
        ParagraphNode paragraph,
        string preview)
    {
        return new RuleProblem(
            message,
            new DocumentLocation
            {
                Paragraph = paragraph.BodyIndex,
                Text = preview
            },
            ParagraphIndexKind.BodyElement,
            new ParagraphAnnotationTarget(paragraph.Paragraph));
    }

    private static bool IsValidIndent(int actualTwips, int[] allowedTwips, int toleranceTwips)
    {
        return allowedTwips.Any(allowed => Math.Abs(actualTwips - allowed) <= toleranceTwips);
    }

    private static string FormatAllowedIndents(ParagraphIndentRuleOptions options)
    {
        var allowedIndentsTwips = options.AllowedIndentTwips ?? [];
        return allowedIndentsTwips.Length == 0
            ? "a configured indent"
            : string.Join(" or ", allowedIndentsTwips.Select(indent => $"{indent / UnitConversion.TwipsPerCm:F2} cm"));
    }

    private static bool StartsWithTabCharacter(Paragraph paragraph)
    {
        foreach (var child in paragraph.Descendants())
        {
            if (child is TabChar)
                return true;

            if (child is Text text)
            {
                var textDecision = StartsWithTextTab(text.Text);
                if (textDecision.HasValue)
                    return textDecision.Value;
            }
        }

        return false;
    }

    private static bool? StartsWithTextTab(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        foreach (var ch in text)
        {
            if (ch == '\t')
                return true;

            if (!char.IsWhiteSpace(ch) && !char.IsControl(ch))
                return false;
        }

        return null;
    }

    private bool IsCenteredOrRightAligned(RuleContext context, Paragraph paragraph)
    {
        var justification = _formattingResolver.ResolveJustification(context.RawDocument, paragraph);
        return justification == JustificationValues.Center || justification == JustificationValues.Right;
    }
}
