using ThesisValidator.Rules;

namespace backend.Application.Validation;

public sealed class SectionContextResolver
{
    public void PopulateSectionContext(
        IReadOnlyList<ValidationIssue> results,
        DocumentContent content)
    {
        var headings = content.BodyChildParagraphs
            .Where(paragraph => paragraph.HeadingLevel is not null)
            .Select(paragraph => (paragraph.BodyIndex, paragraph.Text))
            .ToList();

        foreach (var result in results)
        {
            var paragraphIndex = result.Location?.Paragraph ?? 0;
            if (paragraphIndex <= 0)
                continue;

            var section = FindNearestSection(headings, paragraphIndex);
            if (section is not null)
                result.Location!.Section = section;
        }
    }

    private static string? FindNearestSection(
        List<(int Index, string Text)> headings,
        int paragraphIndex)
    {
        string? nearest = null;
        foreach (var (index, text) in headings)
        {
            if (index <= paragraphIndex)
                nearest = text;
            else
                break;
        }

        return nearest;
    }
}
