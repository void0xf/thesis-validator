using backend.Services.Extraction;
using backend.Services.Formatting;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace backend.Services.Structure;

public static class CaptionDetectionService
{
    private static readonly Regex ValidFigureCaptionRegex = new(
        @"^\s*(?:Rys(?:\.|unek)?|Figure|Fig\.?)\s+\d+(?:\.\d+)*(?:(?:\s*(?::|-|\u2013|\.)\s+\S.*)|(?:\s+\S.*)|\.)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex FigureCaptionAttemptRegex = new(
        @"^\s*(?:Rys(?:\.|unek)?|Figure|Fig\.?|Obrazek|Diagram|Image|Picture)\s*:?\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex FigureSeqInstructionRegex = new(
        @"\bSEQ\s+[""']?(?<label>[\w.]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly IReadOnlySet<string> FigureSeqLabels = new HashSet<string>(
        new[] { "Rys", "Rys.", "Rysunek", "Figure", "Fig", "Fig." }.Select(NormalizeLabel),
        StringComparer.OrdinalIgnoreCase);

    public static bool UsesDedicatedCaptionStyle(Paragraph paragraph)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
        return IsCaptionStyleLabel(styleId);
    }

    public static bool UsesDedicatedCaptionStyle(WordprocessingDocument doc, Paragraph paragraph)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
        var styleName = StyleResolutionService.GetStyleName(doc, styleId);

        return IsCaptionStyleLabel(styleId) || IsCaptionStyleLabel(styleName);
    }

    public static string GetCaptionStyleLabel(Paragraph paragraph)
    {
        return StyleResolutionService.GetParagraphStyleId(paragraph) ?? "Normal";
    }

    public static string GetCaptionStyleLabel(WordprocessingDocument doc, Paragraph paragraph)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
        var styleName = StyleResolutionService.GetStyleName(doc, styleId);

        if (string.IsNullOrWhiteSpace(styleId))
            return "Normal";

        return string.IsNullOrWhiteSpace(styleName)
            ? styleId
            : $"{styleId} ({styleName})";
    }

    public static Run? GetFirstTextRun(Paragraph paragraph, UniversityConfig config)
    {
        return paragraph.Elements<Run>()
            .FirstOrDefault(run => !string.IsNullOrWhiteSpace(TextExtractionService.GetRunText(run, config)));
    }

    public static bool HasValidFigureCaptionFormat(string text)
    {
        return ValidFigureCaptionRegex.IsMatch(text);
    }

    public static bool LooksLikeFigureCaptionText(string text)
    {
        return HasValidFigureCaptionFormat(text)
            || FigureCaptionAttemptRegex.IsMatch(text);
    }

    public static bool IsFigureCaptionCandidate(
        WordprocessingDocument doc,
        Paragraph paragraph,
        UniversityConfig config)
    {
        var text = TextExtractionService.GetParagraphText(doc, paragraph, config).Trim();
        var hasMeaningfulText = TextExtractionService.HasMeaningfulContent(text);

        return HasFigureSequenceField(paragraph)
            || (hasMeaningfulText && LooksLikeFigureCaptionText(text))
            || (hasMeaningfulText && UsesDedicatedCaptionStyle(doc, paragraph));
    }

    public static bool HasFigureSequenceField(Paragraph paragraph)
    {
        var simpleFieldInstructions = paragraph
            .Descendants<SimpleField>()
            .Select(field => field.Instruction?.Value);

        var complexFieldInstructions = paragraph
            .Descendants<FieldCode>()
            .Select(fieldCode => fieldCode.Text);

        return simpleFieldInstructions
            .Concat(complexFieldInstructions)
            .Where(instruction => !string.IsNullOrWhiteSpace(instruction))
            .Any(instruction => IsFigureSequenceInstruction(instruction!));
    }

    public static bool EndsWithSinglePeriod(string text)
    {
        var trimmed = text.TrimEnd();
        return trimmed.EndsWith('.')
            && (trimmed.Length < 2 || trimmed[^2] != '.');
    }

    private static bool IsFigureSequenceInstruction(string instruction)
    {
        var match = FigureSeqInstructionRegex.Match(instruction);
        return match.Success && FigureSeqLabels.Contains(NormalizeLabel(match.Groups["label"].Value));
    }

    private static bool IsCaptionStyleLabel(string? styleLabel)
    {
        if (string.IsNullOrWhiteSpace(styleLabel))
            return false;

        var label = styleLabel.Trim();
        if (IsNormalStyle(label))
            return false;

        return label.Contains("caption", StringComparison.OrdinalIgnoreCase)
            || label.Contains("legend", StringComparison.OrdinalIgnoreCase)
            || label.Contains("legenda", StringComparison.OrdinalIgnoreCase)
            || label.Contains("podpis", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNormalStyle(string styleId)
    {
        return string.Equals(styleId, "Normal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(styleId, "Normalny", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLabel(string label)
    {
        return label.Trim().TrimEnd('.').ToUpperInvariant();
    }
}
