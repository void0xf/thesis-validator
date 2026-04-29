using backend.Models;
using backend.ModernServices;
using backend.RuleOptions;
using backend.Services.Extraction;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

public sealed class FontFamilyRule : ValidationRule<FontFamilyRuleOptions>
{
    public const string RuleId = "FontFamily";

    private const string DefaultRequiredFontFamily = "Times New Roman";
    private readonly ModernFormattingResolver _formattingResolver;

    public FontFamilyRule(ModernFormattingResolver? formattingResolver = null)
    {
        _formattingResolver = formattingResolver ?? new ModernFormattingResolver();
    }

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

                var text = GetRunText(run);
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
                        Text = TextExtractionService.Truncate(text, 50)
                    },
                    ParagraphIndexKind.BodyElement,
                    new RunAnnotationTarget(run));

                characterOffset += text.Length;
            }
        }
    }

    private static string GetRunText(Run run)
    {
        return string.Concat(run.Elements<Text>().Select(text => text.Text));
    }
}
