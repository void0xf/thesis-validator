using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

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
        var body = doc.MainDocumentPart?.Document.Body;

        if (body == null)
            return errors;

        var lists = ExtractLists(body);

        foreach (var list in lists)
        {
            errors.AddRange(ValidatePunctuationConsistency(doc, list, documentCommentService));

            errors.AddRange(ValidateIndentationConsistency(doc, list, documentCommentService));
        }

        return errors;
    }

    private static List<ListGroup> ExtractLists(Body body)
    {
        var lists = new List<ListGroup>();
        ListGroup? currentList = null;
        int? currentNumberingId = null;

        int paragraphIndex = 0;
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraphIndex++;

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
                var indent = GetParagraphIndent(paragraph);

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

            var expectedPunctuation = GetTrailingPunctuation(firstItem.Paragraph);

            foreach (var item in middleItems)
            {
                var ending = GetTrailingPunctuation(item.Paragraph);
                var text = GetParagraphText(item.Paragraph);
                var preview = Truncate(text, 40);

                if (ending != expectedPunctuation)
                {
                    var expectedDesc = expectedPunctuation.HasValue
                        ? $"'{expectedPunctuation}'"
                        : "no punctuation";
                    var actualDesc = ending.HasValue
                        ? $"'{ending}'"
                        : "no punctuation";

                    var errorMessage = $"List item ends with {actualDesc} but first item uses {expectedDesc}. Text: \"{preview}\"";

                    errors.Add(new ValidationResult
                    {
                        RuleName = Name,
                        Message = errorMessage,
                        IsError = true,
                        Location = new DocumentLocation
                        {
                            Paragraph = item.ParagraphIndex,
                            Text = preview
                        }
                    });

                    documentCommentService?.AddCommentToParagraph(doc, item.Paragraph, errorMessage);
                }
            }

            var lastEnding = GetTrailingPunctuation(lastItem.Paragraph);
            if (lastEnding != '.')
            {
                var lastText = GetParagraphText(lastItem.Paragraph);
                var lastPreview = Truncate(lastText, 40);

                var errorMessage = lastEnding.HasValue
                    ? $"Last list item should end with period (.), found '{lastEnding}'. Text: \"{lastPreview}\""
                    : $"Last list item should end with period (.). Text: \"{lastPreview}\"";

                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = errorMessage,
                    IsError = true,
                    Location = new DocumentLocation
                    {
                        Paragraph = lastItem.ParagraphIndex,
                        Text = lastPreview
                    }
                });

                documentCommentService?.AddCommentToParagraph(doc, lastItem.Paragraph, errorMessage);
            }
        }

        return errors;
    }

    private IEnumerable<ValidationResult> ValidateIndentationConsistency(
        WordprocessingDocument doc,
        ListGroup list,
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
                var text = GetParagraphText(item.Paragraph);
                var preview = Truncate(text, 40);

                var expectedCm = TwipsToCm(expectedIndent);
                var actualCm = TwipsToCm(item.IndentLeft);

                var errorMessage = $"List item has inconsistent indentation ({actualCm:F2} cm). " +
                                   $"Expected {expectedCm:F2} cm at level {item.Level}. Text: \"{preview}\"";

                errors.Add(new ValidationResult
                {
                    RuleName = Name,
                    Message = errorMessage,
                    IsError = true,
                    Location = new DocumentLocation
                    {
                        Paragraph = item.ParagraphIndex,
                        Text = preview
                    }
                });

                documentCommentService?.AddCommentToParagraph(doc, item.Paragraph, errorMessage);
            }
        }

        return errors;
    }

    private static int GetParagraphIndent(Paragraph paragraph)
    {
        var indent = paragraph.ParagraphProperties?.Indentation;
        if (indent == null)
            return 0;

        var leftValue = indent.Left?.Value ?? indent.Start?.Value;
        if (leftValue != null && int.TryParse(leftValue, out var left))
            return left;

        return 0;
    }

    private static char? GetTrailingPunctuation(Paragraph paragraph)
    {
        var text = GetParagraphText(paragraph).TrimEnd();
        if (string.IsNullOrEmpty(text))
            return null;

        var lastChar = text[^1];
        return char.IsPunctuation(lastChar) ? lastChar : null;
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static double TwipsToCm(int twips)
    {
        // 1 inch = 1440 twips, 1 inch = 2.54 cm
        return twips / 1440.0 * 2.54;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
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
