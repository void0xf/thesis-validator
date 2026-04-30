using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using backend.DocumentProcessing.Content;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.TablesOfContents;

public static partial class TableOfContentsDetector
{
    private static readonly IReadOnlySet<string> ManualTocNames = new HashSet<string>(
        new[]
        {
            "Spis tre\u015bci",
            "Spis tresci",
            "SPIS TRE\u015aCI",
            "Spis Tre\u015bci",
            "Spis rzeczy",
            "Table of contents",
            "Contents"
        }.Select(NormalizeTocHeading),
        StringComparer.Ordinal);

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public static TableOfContentsDetection Detect(WordprocessingDocument document)
    {
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
            return new TableOfContentsDetection(TableOfContentsKind.None, null, 0, null);

        var automatic = FindAutomaticTableOfContents(body);
        if (automatic is not null)
            return automatic;

        var manual = FindManualTableOfContents(body);
        return manual ?? new TableOfContentsDetection(TableOfContentsKind.None, null, 0, null);
    }

    public static bool IsTableOfContentsHeadingText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return ManualTocNames.Contains(NormalizeTocHeading(text));
    }

    public static bool ContainsTocFieldCode(Paragraph paragraph)
    {
        return paragraph.Descendants<FieldCode>()
            .Any(fieldCode => fieldCode.Text.Trim().StartsWith("TOC", StringComparison.OrdinalIgnoreCase));
    }

    private static TableOfContentsDetection? FindAutomaticTableOfContents(Body body)
    {
        var paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            if (ContainsTocFieldCode(paragraph))
            {
                return new TableOfContentsDetection(
                    TableOfContentsKind.Automatic,
                    paragraph,
                    paragraphIndex,
                    TextExtractor.GetParagraphText(paragraph, skipTextBoxes: false).Trim());
            }
        }

        return null;
    }

    private static TableOfContentsDetection? FindManualTableOfContents(Body body)
    {
        var paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            var text = TextExtractor.GetParagraphText(paragraph, skipTextBoxes: false);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (ManualTocNames.Contains(NormalizeTocHeading(text)))
            {
                return new TableOfContentsDetection(
                    TableOfContentsKind.Manual,
                    paragraph,
                    paragraphIndex,
                    text.Trim());
            }
        }

        return null;
    }

    private static string NormalizeTocHeading(string text)
    {
        var normalized = text.Trim().TrimEnd('.', ':').Trim();
        normalized = RemovePolishDiacritics(normalized);
        normalized = WhitespaceRegex().Replace(normalized, " ");
        return normalized.ToLowerInvariant();
    }

    private static string RemovePolishDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (character == '\u0142')
            {
                builder.Append('l');
                continue;
            }

            if (character == '\u0141')
            {
                builder.Append('L');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
