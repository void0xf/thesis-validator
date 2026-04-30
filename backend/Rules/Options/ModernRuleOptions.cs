namespace backend.RuleOptions;

public enum RuleAvailability
{
    Available,
    Hidden
}

public enum RuleSeverity
{
    Info,
    Warning,
    Error
}

public abstract class RuleOptionsBase
{
    public RuleAvailability Availability { get; set; } = RuleAvailability.Available;
    public RuleSeverity Severity { get; set; } = RuleSeverity.Error;
}

public sealed class EmptySectionStructureRuleOptions : RuleOptionsBase;

public sealed class FigureCaptionFormatRuleOptions : RuleOptionsBase;

public sealed class FigureCaptionPositionRuleOptions : RuleOptionsBase;

public sealed class FontFamilyRuleOptions : RuleOptionsBase
{
    public string? RequiredFontFamily { get; set; }
}

public sealed class GrammarRuleOptions : RuleOptionsBase;

public sealed class HeadingStyleUsageRuleOptions : RuleOptionsBase
{
    public int FontSizeThresholdAboveBodyPt { get; set; } = 2;
    public int MaxHeadingTextLength { get; set; } = 200;
}

public sealed class HierarchyDepthRuleOptions : RuleOptionsBase
{
    public int MaxAllowedLevel { get; set; } = 3;
}

public sealed class LineSpacingDependencyRuleOptions : RuleOptionsBase
{
    public int TargetLineSpacingTwips { get; set; } = 360;
}

public sealed class ListIndentationConsistencyRuleOptions : RuleOptionsBase;

public sealed class ListPunctuationConsistencyRuleOptions : RuleOptionsBase;

public sealed class ManualTableOfContentsRuleOptions : RuleOptionsBase;

public sealed class MissingFigureCaptionRuleOptions : RuleOptionsBase;

public sealed class NoDotsInTitlesRuleOptions : RuleOptionsBase
{
    public string[] TargetStylePatterns { get; set; } =
    [
        "heading",
        "nagwek",
        "title",
        "tytu",
        "subtitle",
        "podtytu",
        "caption",
        "podpis"
    ];
}

public sealed class ParagraphIndentRuleOptions : RuleOptionsBase
{
    public int[] AllowedIndentTwips { get; set; } = [567, 709];

    public int ToleranceTwips { get; set; } = 60;
}

public sealed class ParagraphSpacingRuleOptions : RuleOptionsBase
{
    public int[] AllowedSpacingPoints { get; set; } = [0, 6];
}

public sealed class SingleSpaceRuleOptions : RuleOptionsBase;

public sealed class TextJustificationRuleOptions : RuleOptionsBase;

public sealed class TocRuleOptions : RuleOptionsBase;
