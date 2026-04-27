using backend.Models;
using backend.Rules;
using backend.RuleOptions;
using backend.Services.Results;
using Backend.Models;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Services.Rules;

public sealed class RuleConfigurationService : IRuleConfigurationService
{
    private readonly EmptySectionStructureRuleOptions _emptySectionOptions;
    private readonly FontFamilyRuleOptions _fontFamilyOptions;
    private readonly HeadingStyleUsageRuleOptions _headingStyleUsageOptions;

    public RuleConfigurationService(
        IOptions<EmptySectionStructureRuleOptions> emptySectionOptions,
        IOptions<FontFamilyRuleOptions>? fontFamilyOptions = null,
        IOptions<HeadingStyleUsageRuleOptions>? headingStyleUsageOptions = null)
    {
        _emptySectionOptions = emptySectionOptions.Value;
        _fontFamilyOptions = fontFamilyOptions?.Value ?? new FontFamilyRuleOptions();
        _headingStyleUsageOptions = headingStyleUsageOptions?.Value ?? new HeadingStyleUsageRuleOptions();
    }

    public bool IsRuleAvailable(string ruleId)
    {
        if (IsEmptySectionStructureRule(ruleId))
            return _emptySectionOptions.Availability != RuleAvailability.Hidden;

        if (IsFontFamilyRule(ruleId))
            return _fontFamilyOptions.Availability != RuleAvailability.Hidden;

        if (IsHeadingStyleUsageRule(ruleId))
            return _headingStyleUsageOptions.Availability != RuleAvailability.Hidden;

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

        if (IsHeadingStyleUsageRule(ruleId))
            return ValidationSeverity.Normalize(_headingStyleUsageOptions.Severity.ToString());

        return SeverityResolver.Resolve(ruleId, config, explicitSeverity);
    }

    public RuleDefinition ApplyConfiguration(RuleDefinition definition)
    {
        if (IsEmptySectionStructureRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_emptySectionOptions.Severity.ToString()) };

        if (IsFontFamilyRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_fontFamilyOptions.Severity.ToString()) };

        if (IsHeadingStyleUsageRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_headingStyleUsageOptions.Severity.ToString()) };

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

    private static bool IsHeadingStyleUsageRule(string ruleId)
    {
        return string.Equals(
            ruleId,
            HeadingStyleUsageRule.RuleId,
            StringComparison.OrdinalIgnoreCase);
    }
}
