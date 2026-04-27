using backend.Models;

namespace ThesisValidator.Rules;

public static class RuleCategories
{
    public const string Formatting = "Formatting";
    public const string Layout = "Layout";
    public const string Structure = "Structure";
    public const string Language = "Language";
}

public sealed record RuleDefinition(
    string Id,
    string DisplayName,
    string Category,
    string DefaultSeverity,
    bool Enabled = true,
    bool Selectable = true);

public static class RuleCatalog
{
    private static readonly IReadOnlyDictionary<string, RuleDefinition> Definitions =
        new Dictionary<string, RuleDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["FontFamily"] = new(
                "FontFamily",
                "Font Family",
                RuleCategories.Formatting,
                ValidationSeverity.Error),
            ["SingleSpaceRule"] = new(
                "SingleSpaceRule",
                "Single Space",
                RuleCategories.Formatting,
                ValidationSeverity.Error),
            ["TextJustificationRule"] = new(
                "TextJustificationRule",
                "Text Justification",
                RuleCategories.Formatting,
                ValidationSeverity.Error),
            ["NoDotsInTitlesRule"] = new(
                "NoDotsInTitlesRule",
                "Title Punctuation",
                RuleCategories.Formatting,
                ValidationSeverity.Error),
            ["ListPunctuationConsistencyRule"] = new(
                "ListPunctuationConsistencyRule",
                "List Punctuation Consistency",
                RuleCategories.Layout,
                ValidationSeverity.Error),
            ["ListIndentationConsistencyRule"] = new(
                "ListIndentationConsistencyRule",
                "List Indentation Consistency",
                RuleCategories.Layout,
                ValidationSeverity.Error),
            ["ParagraphSpacingRule"] = new(
                "ParagraphSpacingRule",
                "Paragraph Spacing",
                RuleCategories.Layout,
                ValidationSeverity.Error),
            ["RequiredIndentCm"] = new(
                "RequiredIndentCm",
                "Paragraph Indent",
                RuleCategories.Layout,
                ValidationSeverity.Error),
            ["LineSpacingDependencyRule"] = new(
                "LineSpacingDependencyRule",
                "Line Spacing",
                RuleCategories.Layout,
                ValidationSeverity.Error),
            ["HeadingStyleUsageRule"] = new(
                "HeadingStyleUsageRule",
                "Heading Style Usage",
                RuleCategories.Structure,
                ValidationSeverity.Error),
            ["HierarchyDepthRule"] = new(
                "HierarchyDepthRule",
                "Heading Hierarchy",
                RuleCategories.Structure,
                ValidationSeverity.Error),
            ["MissingFigureCaptionRule"] = new(
                "MissingFigureCaptionRule",
                "Missing Figure Captions",
                RuleCategories.Structure,
                ValidationSeverity.Error),
            ["FigureCaptionPositionRule"] = new(
                "FigureCaptionPositionRule",
                "Figure Caption Position",
                RuleCategories.Structure,
                ValidationSeverity.Error),
            ["FigureCaptionStyleRule"] = new(
                "FigureCaptionStyleRule",
                "Figure Caption Style",
                RuleCategories.Structure,
                ValidationSeverity.Error),
            ["FigureCaptionFormatRule"] = new(
                "FigureCaptionFormatRule",
                "Figure Caption Format",
                RuleCategories.Structure,
                ValidationSeverity.Error),
            ["FigureCaptionAutomaticNumberingRule"] = new(
                "FigureCaptionAutomaticNumberingRule",
                "Figure Caption Automatic Numbering",
                RuleCategories.Structure,
                ValidationSeverity.Warning),
            ["CheckTableOfContents"] = new(
                "CheckTableOfContents",
                "Table of Contents",
                RuleCategories.Structure,
                ValidationSeverity.Error),
            ["Manual table of contents"] = new(
                "Manual table of contents",
                "Manual table of contents",
                RuleCategories.Structure,
                ValidationSeverity.Warning,
                Selectable: false),
            ["EmptySectionStructureRule"] = new(
                "EmptySectionStructureRule",
                "Empty Sections",
                RuleCategories.Language,
                ValidationSeverity.Error),
            ["Grammar"] = new(
                "Grammar",
                "Grammar & Spelling",
                RuleCategories.Language,
                ValidationSeverity.Error)
        };

    public static RuleDefinition GetDefinition(string ruleId)
    {
        return Definitions.TryGetValue(ruleId, out var definition)
            ? definition
            : new RuleDefinition(
                ruleId,
                ruleId,
                RuleCategories.Formatting,
                ValidationSeverity.Error);
    }
}
