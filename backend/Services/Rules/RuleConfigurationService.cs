using backend.Models;
using backend.Rules;
using backend.RuleOptions;
using backend.Services.Results;
using Backend.Models;
using Microsoft.Extensions.Options;
using Rules;
using ThesisValidator.Rules;

namespace backend.Services.Rules;

public sealed class RuleConfigurationService : IRuleConfigurationService
{
    private readonly EmptySectionStructureRuleOptions _emptySectionOptions;
    private readonly FontFamilyRuleOptions _fontFamilyOptions;
    private readonly NoDotsInTitlesRuleOptions _noDotsInTitlesOptions;
    private readonly HeadingStyleUsageRuleOptions _headingStyleUsageOptions;
    private readonly HierarchyDepthRuleOptions _hierarchyDepthOptions;
    private readonly LineSpacingDependencyRuleOptions _lineSpacingDependencyOptions;
    private readonly ParagraphIndentRuleOptions _paragraphIndentOptions;
    private readonly SingleSpaceRuleOptions _singleSpaceOptions;
    private readonly TextJustificationRuleOptions _textJustificationOptions;
    private readonly TocRuleOptions _tocOptions;
    private readonly ManualTableOfContentsRuleOptions _manualTableOfContentsOptions;
    private readonly MissingFigureCaptionRuleOptions _missingFigureCaptionOptions;
    private readonly ListPunctuationConsistencyRuleOptions _listPunctuationConsistencyOptions;
    private readonly ListIndentationConsistencyRuleOptions _listIndentationConsistencyOptions;

    public RuleConfigurationService(
        IOptions<EmptySectionStructureRuleOptions> emptySectionOptions,
        IOptions<FontFamilyRuleOptions>? fontFamilyOptions = null,
        IOptions<NoDotsInTitlesRuleOptions>? noDotsInTitlesOptions = null,
        IOptions<HeadingStyleUsageRuleOptions>? headingStyleUsageOptions = null,
        IOptions<HierarchyDepthRuleOptions>? hierarchyDepthOptions = null,
        IOptions<LineSpacingDependencyRuleOptions>? lineSpacingDependencyOptions = null,
        IOptions<ParagraphIndentRuleOptions>? paragraphIndentOptions = null,
        IOptions<SingleSpaceRuleOptions>? singleSpaceOptions = null,
        IOptions<TextJustificationRuleOptions>? textJustificationOptions = null,
        IOptions<TocRuleOptions>? tocOptions = null,
        IOptions<ManualTableOfContentsRuleOptions>? manualTableOfContentsOptions = null,
        IOptions<MissingFigureCaptionRuleOptions>? missingFigureCaptionOptions = null,
        IOptions<ListPunctuationConsistencyRuleOptions>? listPunctuationConsistencyOptions = null,
        IOptions<ListIndentationConsistencyRuleOptions>? listIndentationConsistencyOptions = null)
    {
        _emptySectionOptions = emptySectionOptions.Value;
        _fontFamilyOptions = fontFamilyOptions?.Value ?? new FontFamilyRuleOptions();
        _noDotsInTitlesOptions = noDotsInTitlesOptions?.Value ?? new NoDotsInTitlesRuleOptions();
        _headingStyleUsageOptions = headingStyleUsageOptions?.Value ?? new HeadingStyleUsageRuleOptions();
        _hierarchyDepthOptions = hierarchyDepthOptions?.Value ?? new HierarchyDepthRuleOptions();
        _lineSpacingDependencyOptions = lineSpacingDependencyOptions?.Value ?? new LineSpacingDependencyRuleOptions();
        _paragraphIndentOptions = paragraphIndentOptions?.Value ?? new ParagraphIndentRuleOptions();
        _singleSpaceOptions = singleSpaceOptions?.Value ?? new SingleSpaceRuleOptions();
        _textJustificationOptions = textJustificationOptions?.Value ?? new TextJustificationRuleOptions();
        _tocOptions = tocOptions?.Value ?? new TocRuleOptions();
        _manualTableOfContentsOptions = manualTableOfContentsOptions?.Value ?? new ManualTableOfContentsRuleOptions();
        _missingFigureCaptionOptions = missingFigureCaptionOptions?.Value ?? new MissingFigureCaptionRuleOptions();
        _listPunctuationConsistencyOptions = listPunctuationConsistencyOptions?.Value ?? new ListPunctuationConsistencyRuleOptions();
        _listIndentationConsistencyOptions = listIndentationConsistencyOptions?.Value ?? new ListIndentationConsistencyRuleOptions();
    }

    public bool IsRuleAvailable(string ruleId)
    {
        if (IsEmptySectionStructureRule(ruleId))
            return _emptySectionOptions.Availability != RuleAvailability.Hidden;

        if (IsFontFamilyRule(ruleId))
            return _fontFamilyOptions.Availability != RuleAvailability.Hidden;

        if (IsNoDotsInTitlesRule(ruleId))
            return _noDotsInTitlesOptions.Availability != RuleAvailability.Hidden;

        if (IsHeadingStyleUsageRule(ruleId))
            return _headingStyleUsageOptions.Availability != RuleAvailability.Hidden;

        if (IsHierarchyDepthRule(ruleId))
            return _hierarchyDepthOptions.Availability != RuleAvailability.Hidden;

        if (IsLineSpacingDependencyRule(ruleId))
            return _lineSpacingDependencyOptions.Availability != RuleAvailability.Hidden;

        if (IsParagraphIndentRule(ruleId))
            return _paragraphIndentOptions.Availability != RuleAvailability.Hidden;

        if (IsSingleSpaceRule(ruleId))
            return _singleSpaceOptions.Availability != RuleAvailability.Hidden;

        if (IsTextJustificationRule(ruleId))
            return _textJustificationOptions.Availability != RuleAvailability.Hidden;

        if (IsTocRule(ruleId))
            return _tocOptions.Availability != RuleAvailability.Hidden;

        if (IsManualTableOfContentsRule(ruleId))
            return _manualTableOfContentsOptions.Availability != RuleAvailability.Hidden;

        if (IsMissingFigureCaptionRule(ruleId))
            return _missingFigureCaptionOptions.Availability != RuleAvailability.Hidden;

        if (IsListPunctuationConsistencyRule(ruleId))
            return _listPunctuationConsistencyOptions.Availability != RuleAvailability.Hidden;

        if (IsListIndentationConsistencyRule(ruleId))
            return _listIndentationConsistencyOptions.Availability != RuleAvailability.Hidden;

        return true;
    }

    public string ResolveSeverity(
        string ruleId,
        UniversityConfig config,
        string? explicitSeverity = null)
    {
        if (IsEmptySectionStructureRule(ruleId))
            return ValidationSeverity.Normalize(_emptySectionOptions.Severity.ToString());

        if (IsFontFamilyRule(ruleId))
            return ValidationSeverity.Normalize(_fontFamilyOptions.Severity.ToString());

        if (IsNoDotsInTitlesRule(ruleId))
            return ValidationSeverity.Normalize(_noDotsInTitlesOptions.Severity.ToString());

        if (IsHeadingStyleUsageRule(ruleId))
            return ValidationSeverity.Normalize(_headingStyleUsageOptions.Severity.ToString());

        if (IsHierarchyDepthRule(ruleId))
            return ValidationSeverity.Normalize(_hierarchyDepthOptions.Severity.ToString());

        if (IsLineSpacingDependencyRule(ruleId))
            return ValidationSeverity.Normalize(_lineSpacingDependencyOptions.Severity.ToString());

        if (IsParagraphIndentRule(ruleId))
            return ValidationSeverity.Normalize(_paragraphIndentOptions.Severity.ToString());

        if (IsSingleSpaceRule(ruleId))
            return ValidationSeverity.Normalize(_singleSpaceOptions.Severity.ToString());

        if (IsTextJustificationRule(ruleId))
            return ValidationSeverity.Normalize(_textJustificationOptions.Severity.ToString());

        if (IsTocRule(ruleId))
            return ValidationSeverity.Normalize(_tocOptions.Severity.ToString());

        if (IsManualTableOfContentsRule(ruleId))
            return ValidationSeverity.Normalize(_manualTableOfContentsOptions.Severity.ToString());

        if (IsMissingFigureCaptionRule(ruleId))
            return ValidationSeverity.Normalize(_missingFigureCaptionOptions.Severity.ToString());

        if (IsListPunctuationConsistencyRule(ruleId))
            return ValidationSeverity.Normalize(_listPunctuationConsistencyOptions.Severity.ToString());

        if (IsListIndentationConsistencyRule(ruleId))
            return ValidationSeverity.Normalize(_listIndentationConsistencyOptions.Severity.ToString());

        return SeverityResolver.Resolve(ruleId, config, explicitSeverity);
    }

    public RuleDefinition ApplyConfiguration(RuleDefinition definition)
    {
        if (IsEmptySectionStructureRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_emptySectionOptions.Severity.ToString()) };

        if (IsFontFamilyRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_fontFamilyOptions.Severity.ToString()) };

        if (IsNoDotsInTitlesRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_noDotsInTitlesOptions.Severity.ToString()) };

        if (IsHeadingStyleUsageRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_headingStyleUsageOptions.Severity.ToString()) };

        if (IsHierarchyDepthRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_hierarchyDepthOptions.Severity.ToString()) };

        if (IsLineSpacingDependencyRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_lineSpacingDependencyOptions.Severity.ToString()) };

        if (IsParagraphIndentRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_paragraphIndentOptions.Severity.ToString()) };

        if (IsSingleSpaceRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_singleSpaceOptions.Severity.ToString()) };

        if (IsTextJustificationRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_textJustificationOptions.Severity.ToString()) };

        if (IsTocRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_tocOptions.Severity.ToString()) };

        if (IsManualTableOfContentsRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_manualTableOfContentsOptions.Severity.ToString()) };

        if (IsMissingFigureCaptionRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_missingFigureCaptionOptions.Severity.ToString()) };

        if (IsListPunctuationConsistencyRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_listPunctuationConsistencyOptions.Severity.ToString()) };

        if (IsListIndentationConsistencyRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_listIndentationConsistencyOptions.Severity.ToString()) };

        return definition;
    }

    private static bool IsEmptySectionStructureRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            EmptySectionStructureRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFontFamilyRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            FontFamilyValidationRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNoDotsInTitlesRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            NoDotsInTitlesRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHeadingStyleUsageRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            HeadingStyleUsageRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHierarchyDepthRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            HierarchyDepthRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLineSpacingDependencyRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            LineSpacingDependencyRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsParagraphIndentRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            ParagraphIndentRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSingleSpaceRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            SingleSpaceRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextJustificationRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            TextJustificationRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTocRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            TocRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsManualTableOfContentsRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            ManualTableOfContentsRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMissingFigureCaptionRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            MissingFigureCaptionRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsListPunctuationConsistencyRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            ListPunctuationConsistencyRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsListIndentationConsistencyRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            ListIndentationConsistencyRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }
}
