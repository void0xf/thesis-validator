using Backend.Models;
using backend.Services.Skipping;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Structure;

public static class FigureDetectionService
{
    public static bool ContainsImage(Paragraph paragraph, UniversityConfig config)
    {
        if (paragraph.Descendants<Drawing>().Any(drawing =>
                !SkipDecisionService.ShouldSkipTextBoxes(config)
                || !TextBoxSkipRule.ContainsTextBoxContent(drawing)))
        {
            return true;
        }

        if (paragraph.Descendants<Picture>().Any(picture =>
                !SkipDecisionService.ShouldSkipTextBoxes(config)
                || !TextBoxSkipRule.ContainsTextBoxContent(picture)))
        {
            return true;
        }

        return false;
    }
}
