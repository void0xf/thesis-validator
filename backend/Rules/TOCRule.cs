using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace Rules;

public enum TableOfContentsKind
{
    None,
    Automatic,
    Manual
}

public sealed record TableOfContentsDetection(
    TableOfContentsKind Kind,
    Paragraph? Paragraph,
    int ParagraphIndex,
    string? Text);

public partial class TocRule : IValidationRule
{
    public const string ManualTableOfContentsRuleName = "Manual table of contents";

    private const string StructureCategory = "Structure";

    private const string ManualTableOfContentsMessage =
        "A table of contents section was detected, but no automatic Word TOC field was found. The table of contents was probably created manually and may become inconsistent with the document structure.";

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

    public string Name => nameof(FormattingConfig.CheckTableOfContents);

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public IEnumerable<ValidationResult> Validate(WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        var errors = new List<ValidationResult>();
        var detection = DetectTableOfContents(doc);

        if (detection.Kind == TableOfContentsKind.Automatic)
            return errors;

        if (detection.Kind == TableOfContentsKind.Manual)
        {
            errors.Add(new ValidationResult
            {
                RuleName = ManualTableOfContentsRuleName,
                Category = StructureCategory,
                Message = ManualTableOfContentsMessage,
                IsError = false,
                Location = new DocumentLocation
                {
                    Paragraph = detection.ParagraphIndex,
                    Text = Truncate(detection.Text ?? string.Empty, 80)
                }
            });

            if (detection.Paragraph is not null && documentCommentService is not null)
                documentCommentService.AddCommentToParagraph(doc, detection.Paragraph, ManualTableOfContentsMessage);

            return errors;
        }

        var body = doc.MainDocumentPart?.Document.Body;
        var firstRun = body?.Descendants<Run>().FirstOrDefault();
        errors.Add(new ValidationResult
        {
            RuleName = Name,
            Category = StructureCategory,
            Message = "Document is missing a Table of Contents.",
            IsError = true
        });

        if (firstRun != null && documentCommentService != null)
            documentCommentService.AddCommentToRun(doc, firstRun, "Document is missing a Table of Contents");

        return errors;
    }

    public static TableOfContentsDetection DetectTableOfContents(WordprocessingDocument doc)
    {
        var body = doc.MainDocumentPart?.Document.Body;
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

    private static TableOfContentsDetection? FindAutomaticTableOfContents(Body body)
    {
        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            if (paragraph.Descendants<FieldCode>().Any(IsTocFieldCode))
            {
                return new TableOfContentsDetection(
                    TableOfContentsKind.Automatic,
                    paragraph,
                    paragraphIndex,
                    GetParagraphText(paragraph).Trim());
            }
        }

        return null;
    }

    private static bool IsTocFieldCode(FieldCode fieldCode)
    {
        return fieldCode.Text.Trim().StartsWith("TOC", StringComparison.OrdinalIgnoreCase);
    }

    private static TableOfContentsDetection? FindManualTableOfContents(Body body)
    {
        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            var text = GetParagraphText(paragraph);
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

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }
}
