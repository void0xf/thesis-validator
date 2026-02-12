using backend.Models;
using backend.Rules;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Services;

public class ThesisValidatorService(IEnumerable<IValidationRule> rules)
{
    private readonly IReadOnlyList<IValidationRule> _ruleList = rules.ToList();

    public (IEnumerable<ValidationResult> Results, List<HeadingInfo> Headings) Validate(Stream fileStream, UniversityConfig config, IEnumerable<string>? selectedRules = null)
    {
        var doc = WordprocessingDocument.Open(fileStream, false);
        var rulesToRun = FilterRules(selectedRules);

        var errors = new List<ValidationResult>();
        foreach (var rule in rulesToRun)
        {
            errors.AddRange(rule.Validate(doc, config));
        }

        var headings = ExtractHeadings(doc);
        var (elementsMap, descendantsMap) = BuildSectionMaps(doc);
        PopulateSectionContext(errors, elementsMap, descendantsMap);

        return (errors, headings);
    }

    public static List<HeadingInfo> ExtractHeadings(WordprocessingDocument doc)
    {
        var headings = new List<HeadingInfo>();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null) return headings;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var level = HeadingStyleHelper.GetHeadingLevel(doc, paragraph);
            if (level is null) continue;

            var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            headings.Add(new HeadingInfo { Level = level.Value, Text = text });
        }

        return headings;
    }

    /// <summary>
    /// Validates the document and adds comments for each error found.
    /// Returns both the validation results and a stream containing the annotated document.
    /// </summary>
    public (IEnumerable<ValidationResult> Results, MemoryStream AnnotatedDocument) ValidateWithComments(Stream fileStream, UniversityConfig config, IEnumerable<string>? selectedRules = null)
    {
        var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var doc = WordprocessingDocument.Open(memoryStream, true);
        var commentService = new DocumentCommentService();
        var rulesToRun = FilterRules(selectedRules);

        var errors = new List<ValidationResult>();
        foreach (var rule in rulesToRun)
        {
            errors.AddRange(rule.Validate(doc, config, commentService));
        }

        doc.MainDocumentPart?.Document.Save();
        var annotatedStream = DocumentCommentService.SaveDocumentWithComments(doc);

        return (errors, annotatedStream);
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
    ) BuildSectionMaps(WordprocessingDocument doc)
    {
        var elementsMap = new List<(int, string)>();
        var descendantsMap = new List<(int, string)>();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null) return (elementsMap, descendantsMap);

        // Elements-based map (direct body children only — matches FontFamily, List, Grammar, FigureCaption, EmptySection rules)
        int elemIdx = 0;
        foreach (var para in body.Elements<Paragraph>())
        {
            elemIdx++;
            var level = HeadingStyleHelper.GetHeadingLevel(doc, para);
            if (level is null) continue;

            var text = string.Concat(para.Descendants<Text>().Select(t => t.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            elementsMap.Add((elemIdx, text));
        }

        // Descendants-based map (all paragraphs incl. table cells — matches SingleSpace, Justification, NoDots, Indent, Spacing, LineSpacing, Hierarchy rules)
        int descIdx = 0;
        foreach (var para in body.Descendants<Paragraph>())
        {
            descIdx++;
            var level = HeadingStyleHelper.GetHeadingLevel(doc, para);
            if (level is null) continue;

            var text = string.Concat(para.Descendants<Text>().Select(t => t.Text)).Trim();
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