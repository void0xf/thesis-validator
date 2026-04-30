using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Formatting;
using ThesisValidator.Rules;

namespace backend.DocumentProcessing.Lists;

public sealed class ListAnalyzer
{
    private readonly FormattingResolver _formattingResolver;
    private readonly ParagraphClassifier _paragraphClassifier;

    public ListAnalyzer(
        FormattingResolver? formattingResolver = null,
        ParagraphClassifier? paragraphClassifier = null)
    {
        _formattingResolver = formattingResolver ?? new FormattingResolver();
        _paragraphClassifier = paragraphClassifier ?? new ParagraphClassifier();
    }

    public IReadOnlyList<ListGroup> ExtractLists(RuleContext context)
    {
        var lists = new List<ListGroup>();
        ListGroup? currentList = null;
        int? currentNumberingId = null;

        foreach (var paragraphNode in context.Content.BodyChildParagraphs)
        {
            var paragraph = paragraphNode.Paragraph;
            if (paragraphNode.IsHeading
                || _paragraphClassifier.HasExcludedStructuralStyle(
                    context.RawDocument,
                    paragraph,
                    excludeListStyles: false))
            {
                currentList = null;
                currentNumberingId = null;
                continue;
            }

            var numberingProps = paragraph.ParagraphProperties?.NumberingProperties;
            var numberingId = numberingProps?.NumberingId?.Val?.Value;

            if (numberingId.HasValue)
            {
                if (currentList is null || currentNumberingId != numberingId)
                {
                    currentList = new ListGroup(numberingId.Value);
                    lists.Add(currentList);
                    currentNumberingId = numberingId;
                }

                var level = numberingProps?.NumberingLevelReference?.Val?.Value ?? 0;
                var indent = _formattingResolver.ResolveLeftIndent(paragraph);

                currentList.Items.Add(new ListItem(
                    paragraph,
                    paragraphNode.BodyIndex,
                    level,
                    indent,
                    paragraphNode.Text));
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
