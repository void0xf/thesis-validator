using backend.DocumentProcessing.TablesOfContents;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Content;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class FontFamilyRule : ValidationRule<FontFamilyRuleOptions>
{
    public const string RuleId = "FontFamilyRule";

    private const string DefaultRequiredFontFamily = "Times New Roman";
    private readonly FormattingResolver _formattingResolver = new();

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Font Family",
        Description: "Finds text runs using a font family different from the required thesis font.",
        Category: RuleCategories.Formatting,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Error);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        FontFamilyRuleOptions options)
    {
        var expectedFont = string.IsNullOrWhiteSpace(options.RequiredFontFamily)
            ? DefaultRequiredFontFamily
            : options.RequiredFontFamily.Trim();

        foreach (var paragraphNode in context.Content.BodyChildParagraphs)
        {
            var runIndex = 0;
            var characterOffset = 0;

            foreach (var run in paragraphNode.Paragraph.Elements<Run>())
            {
                runIndex++;

                var text = TextExtractor.GetRunText(run);
                if (string.IsNullOrWhiteSpace(text))
                {
                    characterOffset += text.Length;
                    continue;
                }

                var actualFont = _formattingResolver.ResolveFontFamily(
                    context.RawDocument,
                    paragraphNode.Paragraph,
                    run);

                if (string.Equals(actualFont, expectedFont, StringComparison.OrdinalIgnoreCase))
                {
                    characterOffset += text.Length;
                    continue;
                }

                var message = $"Invalid font '{actualFont ?? "unknown"}' found, expected '{expectedFont}'";

                yield return new RuleProblem(
                    message,
                    new DocumentLocation
                    {
                        Paragraph = paragraphNode.BodyIndex,
                        Run = runIndex,
                        CharacterOffset = characterOffset,
                        Length = text.Length,
                        Text = TextExtractor.Truncate(text, 50)
                    },
                    ParagraphIndexKind.BodyElement,
                    new RunAnnotationTarget(run));

                characterOffset += text.Length;
            }
        }
    }
}
