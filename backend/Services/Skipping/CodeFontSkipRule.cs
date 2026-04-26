using backend.Services.Formatting;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Skipping;

public sealed class CodeFontSkipRule : ISkipRule
{
    public SkipDecision ShouldSkipRun(
        WordprocessingDocument doc,
        Paragraph paragraph,
        Run run,
        UniversityConfig config,
        SkipContext context)
    {
        if (!SkipDecisionService.ShouldSkipCodeFonts(config))
            return SkipDecision.Include;

        var codeFonts = config.Analysis.CodeFontFamilies;
        if (codeFonts.Count == 0)
            return SkipDecision.Include;

        var fontFamily = FormattingResolutionService.ResolveFontFamily(doc, paragraph, run);
        if (string.IsNullOrWhiteSpace(fontFamily))
            return SkipDecision.Include;

        var shouldSkip = codeFonts.Any(codeFont =>
            string.Equals(codeFont, fontFamily, StringComparison.OrdinalIgnoreCase));

        return shouldSkip
            ? SkipDecision.Skip(SkipReason.CodeFont, $"Run uses configured code font '{fontFamily}'.")
            : SkipDecision.Include;
    }
}
