using backend.Services.Extraction;
using backend.Services.Formatting;
using Backend.Models;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Structure;

public static class CaptionDetectionService
{
    public static bool UsesDedicatedCaptionStyle(Paragraph paragraph)
    {
        var styleId = StyleResolutionService.GetParagraphStyleId(paragraph);
        return !string.IsNullOrEmpty(styleId) && !IsNormalStyle(styleId);
    }

    public static string GetCaptionStyleLabel(Paragraph paragraph)
    {
        return StyleResolutionService.GetParagraphStyleId(paragraph) ?? "Normal";
    }

    public static Run? GetFirstTextRun(Paragraph paragraph, UniversityConfig config)
    {
        return paragraph.Elements<Run>()
            .FirstOrDefault(run => !string.IsNullOrWhiteSpace(TextExtractionService.GetRunText(run, config)));
    }

    private static bool IsNormalStyle(string styleId)
    {
        return string.Equals(styleId, "Normal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(styleId, "Normalny", StringComparison.OrdinalIgnoreCase);
    }
}
