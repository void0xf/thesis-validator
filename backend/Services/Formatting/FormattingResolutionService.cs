using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Formatting;

public static class FormattingResolutionService
{
    public static string? ResolveFontFamily(
        WordprocessingDocument doc,
        Paragraph paragraph,
        Run run)
    {
        var runFont = run.RunProperties?.RunFonts?.Ascii?.Value;
        if (!string.IsNullOrEmpty(runFont))
            return runFont;

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleFont = style.StyleRunProperties?.RunFonts?.Ascii?.Value;
            if (!string.IsNullOrEmpty(styleFont))
                return styleFont;
        }

        var defaultStyleFont = StyleResolutionService
            .GetDefaultParagraphStyle(doc)?
            .StyleRunProperties?
            .RunFonts?
            .Ascii?
            .Value;
        if (!string.IsNullOrEmpty(defaultStyleFont))
            return defaultStyleFont;

        return StyleResolutionService
            .GetDocumentDefaultRunProperties(doc)?
            .RunFonts?
            .Ascii?
            .Value;
    }

    public static double? ResolveFontSizePt(
        WordprocessingDocument doc,
        Paragraph paragraph,
        Run run)
    {
        var runSize = UnitConversion.HalfPointsToPoints(run.RunProperties?.FontSize?.Val?.Value);
        if (runSize is not null)
            return runSize;

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleSize = UnitConversion.HalfPointsToPoints(style.StyleRunProperties?.FontSize?.Val?.Value);
            if (styleSize is not null)
                return styleSize;
        }

        var defaultStyleSize = UnitConversion.HalfPointsToPoints(
            StyleResolutionService.GetDefaultParagraphStyle(doc)?.StyleRunProperties?.FontSize?.Val?.Value);
        if (defaultStyleSize is not null)
            return defaultStyleSize;

        return UnitConversion.HalfPointsToPoints(
            StyleResolutionService.GetDocumentDefaultRunProperties(doc)?.FontSize?.Val?.Value);
    }

    public static bool IsRunBold(WordprocessingDocument doc, Paragraph paragraph, Run run)
    {
        var runBold = GetOnOffValue(run.RunProperties?.Bold);
        if (runBold is not null)
            return runBold.Value;

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleBold = GetOnOffValue(style.StyleRunProperties?.Bold);
            if (styleBold is not null)
                return styleBold.Value;
        }

        var defaultBold = GetOnOffValue(
            StyleResolutionService.GetDocumentDefaultRunProperties(doc)?.Bold);
        return defaultBold ?? false;
    }

    public static JustificationValues ResolveJustification(
        WordprocessingDocument doc,
        Paragraph paragraph,
        bool includeDefaultStyle = true)
    {
        var paragraphJustification = paragraph.ParagraphProperties?.Justification?.Val?.Value;
        if (paragraphJustification.HasValue)
            return paragraphJustification.Value;

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleJustification = style.StyleParagraphProperties?.Justification?.Val?.Value;
            if (styleJustification.HasValue)
                return styleJustification.Value;
        }

        if (includeDefaultStyle)
        {
            var defaultJustification = StyleResolutionService
                .GetDefaultParagraphStyle(doc)?
                .StyleParagraphProperties?
                .Justification?
                .Val?
                .Value;
            if (defaultJustification.HasValue)
                return defaultJustification.Value;

            var documentDefaultJustification = StyleResolutionService
                .GetDocumentDefaultParagraphProperties(doc)?
                .Justification?
                .Val?
                .Value;
            if (documentDefaultJustification.HasValue)
                return documentDefaultJustification.Value;
        }

        return JustificationValues.Left;
    }

    public static int ResolveSpacingAfter(WordprocessingDocument doc, Paragraph paragraph)
    {
        var directAfter = UnitConversion.ParseOptionalTwips(
            paragraph.ParagraphProperties?.SpacingBetweenLines?.After?.Value);
        if (directAfter.HasValue)
            return directAfter.Value;

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleAfter = UnitConversion.ParseOptionalTwips(
                style.StyleParagraphProperties?.SpacingBetweenLines?.After?.Value);
            if (styleAfter.HasValue)
                return styleAfter.Value;
        }

        var defaultAfter = UnitConversion.ParseOptionalTwips(
            StyleResolutionService
                .GetDefaultParagraphStyle(doc)?
                .StyleParagraphProperties?
                .SpacingBetweenLines?
                .After?
                .Value);
        if (defaultAfter.HasValue)
            return defaultAfter.Value;

        return UnitConversion.ParseOptionalTwips(
            StyleResolutionService
                .GetDocumentDefaultParagraphProperties(doc)?
                .SpacingBetweenLines?
                .After?
                .Value) ?? 0;
    }

    public static (int? LineSpacing, LineSpacingRuleValues? LineRule) ResolveLineSpacing(
        WordprocessingDocument doc,
        Paragraph paragraph)
    {
        var direct = ParseLineSpacing(paragraph.ParagraphProperties?.SpacingBetweenLines);
        if (direct.LineSpacing.HasValue)
            return direct;

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleSpacing = ParseLineSpacing(style.StyleParagraphProperties?.SpacingBetweenLines);
            if (styleSpacing.LineSpacing.HasValue)
                return styleSpacing;
        }

        var defaultSpacing = ParseLineSpacing(
            StyleResolutionService.GetDefaultParagraphStyle(doc)?
                .StyleParagraphProperties?
                .SpacingBetweenLines);
        if (defaultSpacing.LineSpacing.HasValue)
            return defaultSpacing;

        return ParseLineSpacing(
            StyleResolutionService.GetDocumentDefaultParagraphProperties(doc)?
                .SpacingBetweenLines);
    }

    public static (int Before, int After) ResolveParagraphSpacing(
        WordprocessingDocument doc,
        Paragraph paragraph)
    {
        var paragraphSpacing = paragraph.ParagraphProperties?.SpacingBetweenLines;
        int? before = UnitConversion.ParseOptionalTwips(paragraphSpacing?.Before?.Value);
        int? after = UnitConversion.ParseOptionalTwips(paragraphSpacing?.After?.Value);

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleSpacing = style.StyleParagraphProperties?.SpacingBetweenLines;
            before ??= UnitConversion.ParseOptionalTwips(styleSpacing?.Before?.Value);
            after ??= UnitConversion.ParseOptionalTwips(styleSpacing?.After?.Value);

            if (before.HasValue && after.HasValue)
                break;
        }

        var defaultSpacing = StyleResolutionService
            .GetDefaultParagraphStyle(doc)?
            .StyleParagraphProperties?
            .SpacingBetweenLines;
        before ??= UnitConversion.ParseOptionalTwips(defaultSpacing?.Before?.Value);
        after ??= UnitConversion.ParseOptionalTwips(defaultSpacing?.After?.Value);

        var documentDefaultSpacing = StyleResolutionService
            .GetDocumentDefaultParagraphProperties(doc)?
            .SpacingBetweenLines;
        before ??= UnitConversion.ParseOptionalTwips(documentDefaultSpacing?.Before?.Value);
        after ??= UnitConversion.ParseOptionalTwips(documentDefaultSpacing?.After?.Value);

        return (before ?? 0, after ?? 0);
    }

    public static int ResolveFirstLineIndent(WordprocessingDocument doc, Paragraph paragraph)
    {
        var paragraphIndentation = paragraph.ParagraphProperties?.Indentation;
        if (HasExplicitFirstLineIndentation(paragraphIndentation))
            return GetFirstLineIndent(paragraphIndentation);

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleIndentation = style.StyleParagraphProperties?.Indentation;
            if (HasExplicitFirstLineIndentation(styleIndentation))
                return GetFirstLineIndent(styleIndentation);
        }

        var defaultStyle = StyleResolutionService.GetDefaultParagraphStyle(doc);
        if (HasExplicitFirstLineIndentation(defaultStyle?.StyleParagraphProperties?.Indentation))
            return GetFirstLineIndent(defaultStyle?.StyleParagraphProperties?.Indentation);

        foreach (var style in StyleResolutionService.GetStyleChain(doc, defaultStyle?.BasedOn?.Val?.Value))
        {
            var styleIndentation = style.StyleParagraphProperties?.Indentation;
            if (HasExplicitFirstLineIndentation(styleIndentation))
                return GetFirstLineIndent(styleIndentation);
        }

        var documentDefaultIndentation = StyleResolutionService
            .GetDocumentDefaultParagraphProperties(doc)?
            .Indentation;
        if (HasExplicitFirstLineIndentation(documentDefaultIndentation))
            return GetFirstLineIndent(documentDefaultIndentation);

        return 0;
    }

    public static (int Left, int FirstLine) ResolveIndentation(
        WordprocessingDocument doc,
        Paragraph paragraph,
        bool includeDefaultStyle = false)
    {
        var directIndentation = paragraph.ParagraphProperties?.Indentation;
        if (directIndentation is not null)
            return ParseIndentation(directIndentation);

        foreach (var style in StyleResolutionService.GetStyleChain(
                     doc,
                     StyleResolutionService.GetParagraphStyleId(paragraph)))
        {
            var styleIndentation = style.StyleParagraphProperties?.Indentation;
            if (styleIndentation is not null)
                return ParseIndentation(styleIndentation);
        }

        if (includeDefaultStyle)
        {
            var defaultIndentation = StyleResolutionService
                .GetDefaultParagraphStyle(doc)?
                .StyleParagraphProperties?
                .Indentation;
            if (defaultIndentation is not null)
                return ParseIndentation(defaultIndentation);
        }

        return (0, 0);
    }

    public static int ResolveLeftIndent(Paragraph paragraph)
    {
        var indentation = paragraph.ParagraphProperties?.Indentation;
        return UnitConversion.ParseTwips(indentation?.Left?.Value ?? indentation?.Start?.Value);
    }

    private static (int? LineSpacing, LineSpacingRuleValues? LineRule) ParseLineSpacing(
        SpacingBetweenLines? spacing)
    {
        var lineSpacing = UnitConversion.ParseOptionalTwips(spacing?.Line?.Value);
        return (lineSpacing, spacing?.LineRule?.Value);
    }

    private static int GetFirstLineIndent(Indentation? indentation)
    {
        if (indentation is null)
            return 0;

        var firstLine = UnitConversion.ParseOptionalTwips(indentation.FirstLine?.Value);
        if (firstLine.HasValue)
            return firstLine.Value;

        if (indentation.FirstLineChars?.Value is not null)
            return (int)(indentation.FirstLineChars.Value * 2.5);

        var hanging = UnitConversion.ParseOptionalTwips(indentation.Hanging?.Value);
        return hanging.HasValue ? -hanging.Value : 0;
    }

    private static bool HasExplicitFirstLineIndentation(Indentation? indentation)
    {
        return indentation?.FirstLine is not null
            || indentation?.Hanging is not null
            || indentation?.FirstLineChars is not null;
    }

    private static (int Left, int FirstLine) ParseIndentation(Indentation indentation)
    {
        var left = UnitConversion.ParseTwips(indentation.Left?.Value ?? indentation.Start?.Value);
        var firstLine = UnitConversion.ParseTwips(indentation.FirstLine?.Value);
        var hanging = UnitConversion.ParseTwips(indentation.Hanging?.Value);

        if (hanging != 0 && firstLine == 0)
            firstLine = -hanging;

        return (left, firstLine);
    }

    private static bool? GetOnOffValue(OnOffType? value)
    {
        if (value is null)
            return null;

        return value.Val is null || value.Val.Value;
    }
}
