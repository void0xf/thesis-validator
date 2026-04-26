using backend.Models;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Formatting;
using backend.Services.Results;
using backend.Services.Skipping;
using backend.Services.Structure;

namespace Rules;

/// <summary>
/// Validates list consistency:
/// 1. Punctuation: Items (except last) should end with same punctuation (; or ,), last item ends with period.
/// 2. Indentation: All items at the same level should have identical indentation.
/// </summary>
public class ListConsistencyRule : IValidationRule
{
    public string Name => "ListConsistencyRule";

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();
        var lists = ExtractLists(doc, config);

        foreach (var list in lists)
        {
            errors.AddRange(ValidatePunctuationConsistency(doc, list, config, documentCommentService));

            errors.AddRange(ValidateIndentationConsistency(doc, list, config, documentCommentService));
        }

        return errors;
    }

    private static List<ListGroup> ExtractLists(WordprocessingDocument doc, UniversityConfig config)
    {
        var lists = new List<ListGroup>();
        ListGroup? currentList = null;
        int? currentNumberingId = null;

        foreach (var (paragraph, paragraphIndex) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            if (HeadingDetectionService.IsHeading(doc, paragraph)
                || SkipDecisionService.HasExcludedStructuralStyle(doc, paragraph))
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

    private IEnumerable<ValidationResult> ValidatePunctuationConsistency(
        WordprocessingDocument doc,
        ListGroup list,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();

        if (list.Items.Count < 2)
            return errors;

        var itemsByLevel = list.Items.GroupBy(i => i.Level);

        foreach (var levelGroup in itemsByLevel)
        {
            var items = levelGroup.ToList();
            if (items.Count < 2)
                continue;

            var firstItem = items.First();
            var lastItem = items.Last();
            var middleItems = items.Count > 2 ? items.Skip(1).Take(items.Count - 2).ToList() : [];

            var expectedPunctuation = GetTrailingPunctuation(firstItem.Paragraph, config);

            foreach (var item in middleItems)
            {
                var ending = GetTrailingPunctuation(item.Paragraph, config);
                var text = TextExtractionService.GetParagraphText(doc, item.Paragraph, config);
                var preview = GetListItemPreview(text);

                if (ending != expectedPunctuation)
                {
                    var expectedDesc = expectedPunctuation.HasValue
                        ? $"'{expectedPunctuation}'"
                        : "no punctuation";
                    var actualDesc = ending.HasValue
                        ? $"'{ending}'"
                        : "no punctuation";

                    var errorMessage = $"List item ends with {actualDesc} but first item uses {expectedDesc}. Text: \"{preview}\"";

                    errors.Add(ValidationResultFactory.ForParagraph(
                        Name,
                        config,
                        errorMessage,
                        item.ParagraphIndex,
                        preview,
                        ParagraphIndexKind.BodyElement));

                    documentCommentService?.AddCommentToParagraph(doc, item.Paragraph, errorMessage);
                }
            }

            var lastEnding = GetTrailingPunctuation(lastItem.Paragraph, config);
            if (lastEnding != '.')
            {
                var lastText = TextExtractionService.GetParagraphText(doc, lastItem.Paragraph, config);
                var lastPreview = GetListItemPreview(lastText);

                var errorMessage = lastEnding.HasValue
                    ? $"Last list item should end with period (.), found '{lastEnding}'. Text: \"{lastPreview}\""
                    : $"Last list item should end with period (.). Text: \"{lastPreview}\"";

                errors.Add(ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    errorMessage,
                    lastItem.ParagraphIndex,
                    lastPreview,
                    ParagraphIndexKind.BodyElement));

                documentCommentService?.AddCommentToParagraph(doc, lastItem.Paragraph, errorMessage);
            }
        }

        return errors;
    }

    private IEnumerable<ValidationResult> ValidateIndentationConsistency(
        WordprocessingDocument doc,
        ListGroup list,
        UniversityConfig config,
        DocumentCommentService? documentCommentService)
    {
        var errors = new List<ValidationResult>();

        var itemsByLevel = list.Items.GroupBy(i => i.Level);

        foreach (var levelGroup in itemsByLevel)
        {
            var items = levelGroup.ToList();
            if (items.Count < 2)
                continue;

            var indentCounts = items
                .GroupBy(i => i.IndentLeft)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (indentCounts.Count <= 1)
                continue;

            var expectedIndent = indentCounts.First().Key;

            foreach (var item in items.Where(i => i.IndentLeft != expectedIndent))
            {
                var text = TextExtractionService.GetParagraphText(doc, item.Paragraph, config);
                var preview = TextExtractionService.Truncate(text, 40);

                var expectedCm = UnitConversion.TwipsToCentimeters(expectedIndent);
                var actualCm = UnitConversion.TwipsToCentimeters(item.IndentLeft);

                var errorMessage = $"List item has inconsistent indentation ({actualCm:F2} cm). " +
                                   $"Expected {expectedCm:F2} cm at level {item.Level}. Text: \"{preview}\"";

                errors.Add(ValidationResultFactory.ForParagraph(
                    Name,
                    config,
                    errorMessage,
                    item.ParagraphIndex,
                    preview,
                    ParagraphIndexKind.BodyElement));

                documentCommentService?.AddCommentToParagraph(doc, item.Paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static char? GetTrailingPunctuation(Paragraph paragraph, UniversityConfig config)
    {
        var text = TextExtractionService.GetParagraphText(paragraph, config).TrimEnd();
        if (string.IsNullOrEmpty(text))
            return null;

        var lastChar = text[^1];
        return char.IsPunctuation(lastChar) ? lastChar : null;
    }

    private static string GetListItemPreview(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? "[empty]"
            : TextExtractionService.Truncate(text, 40);
    }

    private class ListGroup
    {
        public int NumberingId { get; set; }
        public List<ListItem> Items { get; } = [];
    }

    private class ListItem
    {
        public required Paragraph Paragraph { get; init; }
        public int ParagraphIndex { get; init; }
        public int Level { get; init; }
        public int IndentLeft { get; init; }
    }
}
