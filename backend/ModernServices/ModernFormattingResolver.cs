using backend.Services.Formatting;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.ModernServices;

public sealed class ModernFormattingResolver
{
    public string? ResolveFontFamily(
        WordprocessingDocument document,
        Paragraph paragraph,
        Run run)
    {
        var runFont = run.RunProperties?.RunFonts?.Ascii?.Value;
        if (!string.IsNullOrEmpty(runFont))
            return runFont;

        foreach (var style in StyleResolutionService.GetStyleChain(
                     document,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleFont = style.StyleRunProperties?.RunFonts?.Ascii?.Value;
            if (!string.IsNullOrEmpty(styleFont))
                return styleFont;
        }

        var defaultStyleFont = StyleResolutionService
            .GetDefaultParagraphStyle(document)?
            .StyleRunProperties?
            .RunFonts?
            .Ascii?
            .Value;
        if (!string.IsNullOrEmpty(defaultStyleFont))
            return defaultStyleFont;

        return StyleResolutionService
            .GetDocumentDefaultRunProperties(document)?
            .RunFonts?
            .Ascii?
            .Value;
    }
}
