using backend.DocumentProcessing.Formatting;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.Formatting;

public sealed class FormattingResolver
{
    public string? ResolveFontFamily(
        WordprocessingDocument document,
        Paragraph paragraph,
        Run run)
    {
        var runFont = run.RunProperties?.RunFonts?.Ascii?.Value;
        if (!string.IsNullOrEmpty(runFont))
            return runFont;

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleFont = style.StyleRunProperties?.RunFonts?.Ascii?.Value;
            if (!string.IsNullOrEmpty(styleFont))
                return styleFont;
        }

        var defaultStyleFont = StyleResolver
            .GetDefaultParagraphStyle(document)?
            .StyleRunProperties?
            .RunFonts?
            .Ascii?
            .Value;
        if (!string.IsNullOrEmpty(defaultStyleFont))
            return defaultStyleFont;

        return StyleResolver
            .GetDocumentDefaultRunProperties(document)?
            .RunFonts?
            .Ascii?
            .Value;
    }

    public double? ResolveFontSizePt(
        WordprocessingDocument document,
        Paragraph paragraph,
        Run run)
    {
        var runSize = UnitConversion.HalfPointsToPoints(run.RunProperties?.FontSize?.Val?.Value);
        if (runSize is not null)
            return runSize;

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleSize = UnitConversion.HalfPointsToPoints(style.StyleRunProperties?.FontSize?.Val?.Value);
            if (styleSize is not null)
                return styleSize;
        }

        var defaultStyleSize = UnitConversion.HalfPointsToPoints(
            StyleResolver.GetDefaultParagraphStyle(document)?.StyleRunProperties?.FontSize?.Val?.Value);
        if (defaultStyleSize is not null)
            return defaultStyleSize;

        return UnitConversion.HalfPointsToPoints(
            StyleResolver.GetDocumentDefaultRunProperties(document)?.FontSize?.Val?.Value);
    }

    public bool IsRunBold(
        WordprocessingDocument document,
        Paragraph paragraph,
        Run run)
    {
        var runBold = GetOnOffValue(run.RunProperties?.Bold);
        if (runBold is not null)
            return runBold.Value;

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleBold = GetOnOffValue(style.StyleRunProperties?.Bold);
            if (styleBold is not null)
                return styleBold.Value;
        }

        var defaultBold = GetOnOffValue(
            StyleResolver.GetDocumentDefaultRunProperties(document)?.Bold);
        return defaultBold ?? false;
    }

    public JustificationValues ResolveJustification(
        WordprocessingDocument document,
        Paragraph paragraph,
        bool includeDefaultStyle = true)
    {
        var paragraphJustification = paragraph.ParagraphProperties?.Justification?.Val?.Value;
        if (paragraphJustification.HasValue)
            return paragraphJustification.Value;

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleJustification = style.StyleParagraphProperties?.Justification?.Val?.Value;
            if (styleJustification.HasValue)
                return styleJustification.Value;
        }

        if (includeDefaultStyle)
        {
            var defaultJustification = StyleResolver
                .GetDefaultParagraphStyle(document)?
                .StyleParagraphProperties?
                .Justification?
                .Val?
                .Value;
            if (defaultJustification.HasValue)
                return defaultJustification.Value;

            var documentDefaultJustification = StyleResolver
                .GetDocumentDefaultParagraphProperties(document)?
                .Justification?
                .Val?
                .Value;
            if (documentDefaultJustification.HasValue)
                return documentDefaultJustification.Value;
        }

        return JustificationValues.Left;
    }

    public int ResolveSpacingAfter(
        WordprocessingDocument document,
        Paragraph paragraph)
    {
        var directAfter = UnitConversion.ParseOptionalTwips(
            paragraph.ParagraphProperties?.SpacingBetweenLines?.After?.Value);
        if (directAfter.HasValue)
            return directAfter.Value;

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleAfter = UnitConversion.ParseOptionalTwips(
                style.StyleParagraphProperties?.SpacingBetweenLines?.After?.Value);
            if (styleAfter.HasValue)
                return styleAfter.Value;
        }

        var defaultAfter = UnitConversion.ParseOptionalTwips(
            StyleResolver
                .GetDefaultParagraphStyle(document)?
                .StyleParagraphProperties?
                .SpacingBetweenLines?
                .After?
                .Value);
        if (defaultAfter.HasValue)
            return defaultAfter.Value;

        return UnitConversion.ParseOptionalTwips(
            StyleResolver
                .GetDocumentDefaultParagraphProperties(document)?
                .SpacingBetweenLines?
                .After?
                .Value) ?? 0;
    }

    public (int? LineSpacing, LineSpacingRuleValues? LineRule) ResolveLineSpacing(
        WordprocessingDocument document,
        Paragraph paragraph)
    {
        var direct = ParseLineSpacing(paragraph.ParagraphProperties?.SpacingBetweenLines);
        if (direct.LineSpacing.HasValue)
            return direct;

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleSpacing = ParseLineSpacing(style.StyleParagraphProperties?.SpacingBetweenLines);
            if (styleSpacing.LineSpacing.HasValue)
                return styleSpacing;
        }

        var defaultSpacing = ParseLineSpacing(
            StyleResolver.GetDefaultParagraphStyle(document)?
                .StyleParagraphProperties?
                .SpacingBetweenLines);
        if (defaultSpacing.LineSpacing.HasValue)
            return defaultSpacing;

        return ParseLineSpacing(
            StyleResolver.GetDocumentDefaultParagraphProperties(document)?
                .SpacingBetweenLines);
    }

    public int ResolveFirstLineIndent(
        WordprocessingDocument document,
        Paragraph paragraph)
    {
        var paragraphIndentation = paragraph.ParagraphProperties?.Indentation;
        if (HasExplicitFirstLineIndentation(paragraphIndentation))
            return GetFirstLineIndent(paragraphIndentation);

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleIndentation = style.StyleParagraphProperties?.Indentation;
            if (HasExplicitFirstLineIndentation(styleIndentation))
                return GetFirstLineIndent(styleIndentation);
        }

        var defaultStyle = StyleResolver.GetDefaultParagraphStyle(document);
        if (HasExplicitFirstLineIndentation(defaultStyle?.StyleParagraphProperties?.Indentation))
            return GetFirstLineIndent(defaultStyle?.StyleParagraphProperties?.Indentation);

        foreach (var style in StyleResolver.GetStyleChain(document, defaultStyle?.BasedOn?.Val?.Value))
        {
            var styleIndentation = style.StyleParagraphProperties?.Indentation;
            if (HasExplicitFirstLineIndentation(styleIndentation))
                return GetFirstLineIndent(styleIndentation);
        }

        var documentDefaultIndentation = StyleResolver
            .GetDocumentDefaultParagraphProperties(document)?
            .Indentation;
        if (HasExplicitFirstLineIndentation(documentDefaultIndentation))
            return GetFirstLineIndent(documentDefaultIndentation);

        return 0;
    }

    public (int Left, int FirstLine) ResolveIndentation(
        WordprocessingDocument document,
        Paragraph paragraph,
        bool includeDefaultStyle = false)
    {
        var paragraphIndentation = paragraph.ParagraphProperties?.Indentation;
        if (paragraphIndentation is not null)
            return ParseIndentation(paragraphIndentation);

        foreach (var style in StyleResolver.GetStyleChain(
                     document,
                     StyleResolver.GetParagraphStyleId(paragraph)))
        {
            var styleIndentation = style.StyleParagraphProperties?.Indentation;
            if (styleIndentation is not null)
                return ParseIndentation(styleIndentation);
        }

        if (includeDefaultStyle)
        {
            var defaultIndentation = StyleResolver
                .GetDefaultParagraphStyle(document)?
                .StyleParagraphProperties?
                .Indentation;
            if (defaultIndentation is not null)
                return ParseIndentation(defaultIndentation);
        }

        return (0, 0);
    }

    public int ResolveLeftIndent(Paragraph paragraph)
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
