using backend.Models;
using backend.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rules;
using ThesisValidator.Rules;

namespace backend.Services;

public class ThesisValidatorService
{
    private readonly IReadOnlyList<IValidationRule> _ruleList;
    private readonly IReadOnlySet<string> _ruleNames;

    public ThesisValidatorService(IEnumerable<IValidationRule> rules)
    {
        _ruleList = rules.ToList();
        _ruleNames = _ruleList.Select(rule => rule.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetAvailableRuleNames()
    {
        return _ruleList.Select(rule => rule.Name).ToList();
    }

    public IReadOnlyList<string> GetUnknownRuleNames(IEnumerable<string> selectedRules)
    {
        return selectedRules
            .Where(ruleName => !_ruleNames.Contains(ruleName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public (IEnumerable<ValidationResult> Results, List<HeadingInfo> Headings) Validate(Stream fileStream, UniversityConfig config, IEnumerable<string>? selectedRules = null)
    {
        using var doc = OpenDocument(fileStream, isEditable: false);
        var rulesToRun = FilterRules(selectedRules);

        var errors = new List<ValidationResult>();
        foreach (var rule in rulesToRun)
        {
            errors.AddRange(rule.Validate(doc, config));
        }

        var headings = ExtractHeadings(doc, config);
        var (elementsMap, descendantsMap) = BuildSectionMaps(doc, config);
        PopulateSectionContext(errors, elementsMap, descendantsMap);

        return (errors, headings);
    }

    public static List<HeadingInfo> ExtractHeadings(WordprocessingDocument doc, UniversityConfig? config = null)
    {
        var headings = new List<HeadingInfo>();
        var effectiveConfig = config ?? new UniversityConfig();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return headings;

        var tocParagraphIndex = effectiveConfig.Formatting.SkipBeforeTableOfContents
            ? TocRule.DetectTableOfContents(doc).ParagraphIndex
            : 0;

        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            var text = DocumentAnalysisScope.GetParagraphText(paragraph, effectiveConfig).Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (TocRule.IsTableOfContentsHeadingText(text))
            {
                headings.Add(new HeadingInfo { Level = 1, Text = text });
                continue;
            }

            if (tocParagraphIndex > 0 && paragraphIndex <= tocParagraphIndex)
                continue;

            var level = HeadingStyleHelper.GetHeadingLevel(doc, paragraph);
            if (level is null) continue;

            headings.Add(new HeadingInfo { Level = level.Value, Text = text });
        }

        return headings;
    }

    /// <summary>
    /// Validates the document and adds comments for each error found.
    /// Returns both the validation results and a stream containing the annotated document.
    /// </summary>
    public (IEnumerable<ValidationResult> Results, MemoryStream AnnotatedDocument)
    ValidateWithComments(Stream fileStream, UniversityConfig config, IEnumerable<string>? selectedRules = null)
    {
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var doc = OpenDocument(memoryStream, isEditable: true);
        var commentService = new DocumentCommentService();
        var rulesToRun = FilterRules(selectedRules);

        var errors = new List<ValidationResult>();
        foreach (var rule in rulesToRun)
        {
            errors.AddRange(rule.Validate(doc, config, commentService));
        }

        try
        {
            doc.MainDocumentPart?.Document.Save();
            var annotatedStream = DocumentCommentService.SaveDocumentWithComments(doc);

            return (errors, annotatedStream);
        }
        catch (Exception ex) when (IsDocumentProcessingException(ex))
        {
            throw new InvalidThesisDocumentException("The uploaded file could not be saved as an annotated DOCX document.", ex);
        }
    }

    private static WordprocessingDocument OpenDocument(Stream stream, bool isEditable)
    {
        try
        {
            var doc = WordprocessingDocument.Open(stream, isEditable);
            if (doc.MainDocumentPart?.Document is not null)
            {
                return doc;
            }

            doc.Dispose();
            throw new InvalidThesisDocumentException("The uploaded file is not a valid Wordprocessing document.");
        }
        catch (InvalidThesisDocumentException)
        {
            throw;
        }
        catch (Exception ex) when (IsDocumentProcessingException(ex))
        {
            throw new InvalidThesisDocumentException("The uploaded file could not be opened as a DOCX document.", ex);
        }
    }

    private static bool IsDocumentProcessingException(Exception exception)
    {
        return exception is OpenXmlPackageException
            or FileFormatException
            or InvalidDataException
            or IOException
            or NotSupportedException
            or InvalidOperationException
            or ArgumentException
            or ObjectDisposedException;
    }

    /// <summary>
    /// Builds two sorted heading maps — one keyed by direct-child paragraph index
    /// (<c>Elements&lt;Paragraph&gt;</c>) and one by all-descendants index
    /// (<c>Descendants&lt;Paragraph&gt;</c>).  Different rules use different traversals,
    /// so we need both to reliably match a result's paragraph index to a heading.
    /// </summary>
    private static (
        List<(int Index, string Text)> ElementsMap,
        List<(int Index, string Text)> DescendantsMap
    ) BuildSectionMaps(WordprocessingDocument doc, UniversityConfig config)
    {
        var elementsMap = new List<(int, string)>();
        var descendantsMap = new List<(int, string)>();

        // Elements-based map (direct body children only — matches FontFamily, List, Grammar, FigureCaption, EmptySection rules)
        foreach (var (para, elemIdx) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            var level = HeadingStyleHelper.GetHeadingLevel(doc, para);
            if (level is null) continue;

            var text = DocumentAnalysisScope.GetParagraphText(para, config).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            elementsMap.Add((elemIdx, text));
        }

        // Descendants-based map (all paragraphs incl. table cells — matches SingleSpace, Justification, NoDots, Indent, Spacing, LineSpacing, Hierarchy rules)
        foreach (var (para, descIdx) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            var level = HeadingStyleHelper.GetHeadingLevel(doc, para);
            if (level is null) continue;

            var text = DocumentAnalysisScope.GetParagraphText(para, config).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            descendantsMap.Add((descIdx, text));
        }

        return (elementsMap, descendantsMap);
    }

    /// <summary>
    /// Rules that iterate <c>body.Elements&lt;Paragraph&gt;()</c> (direct children only).
    /// All other rules use <c>body.Descendants&lt;Paragraph&gt;()</c>.
    /// </summary>
    private static readonly HashSet<string> ElementsBasedRules = new(StringComparer.OrdinalIgnoreCase)
    {
        "FontFamily",                // FontFamilyValidationRule
        "ListConsistencyRule",
        "Grammar",                   // GrammarRule
        "FigureCaptionStyleRule",
        "EmptySectionStructureRule",
        "HeadingStyleUsageRule",
        "CheckTableOfContents",      // TocRule
    };

    /// <summary>
    /// For each validation result, finds the nearest preceding heading and
    /// writes it into <see cref="DocumentLocation.Section"/>.
    /// Uses the correct section map based on which paragraph traversal the rule uses.
    /// </summary>
    private static void PopulateSectionContext(
        List<ValidationResult> results,
        List<(int Index, string Text)> elementsMap,
        List<(int Index, string Text)> descendantsMap)
    {
        foreach (var result in results)
        {
            var paraIdx = result.Location?.Paragraph ?? 0;
            if (paraIdx <= 0) continue;

            var map = ElementsBasedRules.Contains(result.RuleName)
                ? elementsMap
                : descendantsMap;

            var section = FindNearestSection(map, paraIdx);

            if (section is not null)
                result.Location!.Section = section;
        }
    }

    private static string? FindNearestSection(List<(int Index, string Text)> map, int paraIdx)
    {
        string? nearest = null;
        foreach (var (idx, text) in map)
        {
            if (idx <= paraIdx)
                nearest = text;
            else
                break;
        }
        return nearest;
    }

    private IReadOnlyList<IValidationRule> FilterRules(IEnumerable<string>? selectedRules)
    {
        if (selectedRules is null)
            return _ruleList;

        var selectedSet = selectedRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selectedSet.Count == 0)
            return _ruleList;

        return _ruleList.Where(r => selectedSet.Contains(r.Name)).ToList();
    }
}
