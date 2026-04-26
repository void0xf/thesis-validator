using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Skipping;

public interface ISkipRule
{
    SkipDecision ShouldSkipParagraph(
        WordprocessingDocument doc,
        Paragraph paragraph,
        UniversityConfig config,
        SkipContext context)
    {
        return SkipDecision.Include;
    }

    SkipDecision ShouldSkipRun(
        WordprocessingDocument doc,
        Paragraph paragraph,
        Run run,
        UniversityConfig config,
        SkipContext context)
    {
        return SkipDecision.Include;
    }

    SkipDecision ShouldSkipElement(
        OpenXmlElement element,
        UniversityConfig config,
        SkipContext context)
    {
        return SkipDecision.Include;
    }
}
