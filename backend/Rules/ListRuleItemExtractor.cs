using backend.Services.Analysis;
using backend.Services.Formatting;
using backend.Services.Skipping;
using backend.Services.Structure;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Rules;

internal static class ListRuleItemExtractor
{
    public static List<ListGroup> ExtractLists(WordprocessingDocument doc, UniversityConfig config)
    {
        var lists = new List<ListGroup>();
        ListGroup? currentList = null;
        int? currentNumberingId = null;

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph)
                || SkipDecisionService.HasExcludedStructuralStyle(doc, paragraph, excludeListStyles: false))
            {
                currentList = null;
                currentNumberingId = null;
                continue;
            }

            var numberingProps = paragraph.ParagraphProperties?.NumberingProperties;
            var numberingId = numberingProps?.NumberingId?.Val?.Value;

            if (numberingId.HasValue)
            {
                if (currentList == null || currentNumberingId != numberingId)
                {
                    currentList = new ListGroup { NumberingId = numberingId.Value };
                    lists.Add(currentList);
                    currentNumberingId = numberingId;
                }

                var level = numberingProps?.NumberingLevelReference?.Val?.Value ?? 0;
                var indent = FormattingResolutionService.ResolveLeftIndent(paragraph);

                currentList.Items.Add(new ListItem
                {
                    Paragraph = paragraph,
                    ParagraphIndex = paragraphIndex,
                    Level = level,
                    IndentLeft = indent
                });
            }
            else
            {
                currentList = null;
                currentNumberingId = null;
            }
        }

        return lists;
    }
}

internal sealed class ListGroup
{
    public int NumberingId { get; set; }
    public List<ListItem> Items { get; } = [];
}

internal sealed class ListItem
{
    public required Paragraph Paragraph { get; init; }
    public int ParagraphIndex { get; init; }
    public int Level { get; init; }
    public int IndentLeft { get; init; }
}
