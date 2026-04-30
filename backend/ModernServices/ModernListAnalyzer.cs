using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.ModernServices;

public sealed class ModernListAnalyzer
{
    private readonly ModernFormattingResolver _formattingResolver;
    private readonly ModernParagraphClassifier _paragraphClassifier;

    public ModernListAnalyzer(
        ModernFormattingResolver? formattingResolver = null,
        ModernParagraphClassifier? paragraphClassifier = null)
    {
        _formattingResolver = formattingResolver ?? new ModernFormattingResolver();
        _paragraphClassifier = paragraphClassifier ?? new ModernParagraphClassifier();
    }

    public IReadOnlyList<ModernListGroup> ExtractLists(RuleContext context)
    {
        var lists = new List<ModernListGroup>();
        ModernListGroup? currentList = null;
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
                    currentList = new ModernListGroup(numberingId.Value);
                    lists.Add(currentList);
                    currentNumberingId = numberingId;
                }

                var level = numberingProps?.NumberingLevelReference?.Val?.Value ?? 0;
                var indent = _formattingResolver.ResolveLeftIndent(paragraph);

                currentList.Items.Add(new ModernListItem(
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

public sealed class ModernListGroup
{
    public ModernListGroup(int numberingId)
    {
        NumberingId = numberingId;
    }

    public int NumberingId { get; }

    public List<ModernListItem> Items { get; } = [];
}

public sealed record ModernListItem(
    Paragraph Paragraph,
    int ParagraphIndex,
    int Level,
    int IndentLeft,
    string Text);
