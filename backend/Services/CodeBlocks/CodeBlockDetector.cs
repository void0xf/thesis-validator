using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;

namespace backend.Services.CodeBlocks;

public sealed class CodeBlockDetector : ICodeBlockDetector
{
    private readonly CodeBlockDetectionOptions _options;
    private readonly HashSet<string> _codeFonts;

    public CodeBlockDetector(IOptions<CodeBlockDetectionOptions> options)
    {
        _options = options.Value;
        _codeFonts = (_options.CodeFonts ?? [])
            .Where(font => !string.IsNullOrWhiteSpace(font))
            .Select(font => font.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static ICodeBlockDetector CreateDefault()
    {
        return new CodeBlockDetector(Options.Create(new CodeBlockDetectionOptions()));
    }

    public bool IsCodeBlock(Paragraph paragraph, MainDocumentPart mainPart)
    {
        return Analyze(paragraph, mainPart).IsCodeBlock;
    }

    public CodeBlockDetectionResult Analyze(Paragraph paragraph, MainDocumentPart mainPart)
    {
        var detectedFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? matchedFont = null;
        var totalTextLength = 0;
        var codeFontTextLength = 0;

        foreach (var run in paragraph.Descendants<Run>())
        {
            var text = GetRunText(run);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            totalTextLength += text.Length;

            var fonts = ResolveFontCandidates(run, paragraph, mainPart)
                .Where(font => !string.IsNullOrWhiteSpace(font))
                .Select(font => font.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var font in fonts)
            {
                detectedFonts.Add(font);
            }

            var runMatchedFont = fonts.FirstOrDefault(font => _codeFonts.Contains(font));
            if (runMatchedFont is not null)
            {
                matchedFont ??= runMatchedFont;
                codeFontTextLength += text.Length;
            }
        }

        if (totalTextLength == 0)
        {
            return new CodeBlockDetectionResult
            {
                DetectedFonts = detectedFonts.Order(StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        var ratio = codeFontTextLength / (double)totalTextLength;
        var isCodeBlock = _options.RequireWholeParagraphMonospace
            ? codeFontTextLength == totalTextLength
            : ratio >= _options.MinimumCodeFontTextRatio;

        return new CodeBlockDetectionResult
        {
            IsCodeBlock = isCodeBlock,
            CodeFontTextRatio = ratio,
            DetectedFonts = detectedFonts.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            MatchedFont = matchedFont,
            TotalTextLength = totalTextLength,
            CodeFontTextLength = codeFontTextLength
        };
    }

    private static string GetRunText(Run run)
    {
        return string.Concat(run.Elements<Text>().Select(text => text.Text));
    }

    private static IEnumerable<string> ResolveFontCandidates(
        Run run,
        Paragraph paragraph,
        MainDocumentPart mainPart)
    {
        var directFonts = GetRunFontValues(run.RunProperties?.RunFonts).ToList();
        if (directFonts.Count > 0)
            return directFonts;

        var runStyleFonts = GetStyleRunFonts(
                mainPart,
                run.RunProperties?.RunStyle?.Val?.Value,
                StyleValues.Character)
            .ToList();
        if (runStyleFonts.Count > 0)
            return runStyleFonts;

        var paragraphStyleFonts = GetStyleRunFonts(
                mainPart,
                paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value,
                StyleValues.Paragraph)
            .ToList();
        if (paragraphStyleFonts.Count > 0)
            return paragraphStyleFonts;

        var defaultParagraphStyleFonts = GetDefaultParagraphStyleRunFonts(mainPart).ToList();
        if (defaultParagraphStyleFonts.Count > 0)
            return defaultParagraphStyleFonts;

        return GetRunFontValues(
            mainPart.StyleDefinitionsPart?
                .Styles?
                .DocDefaults?
                .RunPropertiesDefault?
                .RunPropertiesBaseStyle?
                .RunFonts);
    }

    private static IEnumerable<string> GetStyleRunFonts(
        MainDocumentPart mainPart,
        string? styleId,
        StyleValues expectedStyleType)
    {
        foreach (var style in GetStyleChain(mainPart, styleId, expectedStyleType))
        {
            var fonts = GetRunFontValues(style.StyleRunProperties?.RunFonts).ToList();
            if (fonts.Count > 0)
                return fonts;
        }

        return Array.Empty<string>();
    }

    private static IEnumerable<string> GetDefaultParagraphStyleRunFonts(MainDocumentPart mainPart)
    {
        var style = mainPart.StyleDefinitionsPart?
            .Styles?
            .Elements<Style>()
            .FirstOrDefault(style =>
                style.Type?.Value == StyleValues.Paragraph
                && style.Default?.Value == true);

        return GetRunFontValues(style?.StyleRunProperties?.RunFonts);
    }

    private static IEnumerable<Style> GetStyleChain(
        MainDocumentPart mainPart,
        string? styleId,
        StyleValues expectedStyleType)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentStyleId = styleId;

        while (!string.IsNullOrWhiteSpace(currentStyleId) && visited.Add(currentStyleId))
        {
            var style = mainPart.StyleDefinitionsPart?
                .Styles?
                .Elements<Style>()
                .FirstOrDefault(style =>
                    string.Equals(style.StyleId?.Value, currentStyleId, StringComparison.OrdinalIgnoreCase)
                    && style.Type?.Value == expectedStyleType);

            if (style is null)
                yield break;

            yield return style;
            currentStyleId = style.BasedOn?.Val?.Value;
        }
    }

    private static IEnumerable<string> GetRunFontValues(RunFonts? runFonts)
    {
        if (runFonts is null)
            yield break;

        foreach (var font in NonEmpty(
                     runFonts.Ascii?.Value,
                     runFonts.HighAnsi?.Value,
                     runFonts.ComplexScript?.Value,
                     runFonts.EastAsia?.Value))
        {
            yield return font;
        }

        // TODO: Resolve theme font values to concrete theme fonts if code blocks are styled through document themes.
        foreach (var themeFont in NonEmpty(
                     runFonts.AsciiTheme?.Value.ToString(),
                     runFonts.HighAnsiTheme?.Value.ToString(),
                     runFonts.ComplexScriptTheme?.Value.ToString(),
                     runFonts.EastAsiaTheme?.Value.ToString()))
        {
            yield return themeFont;
        }
    }

    private static IEnumerable<string> NonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }
    }
}
