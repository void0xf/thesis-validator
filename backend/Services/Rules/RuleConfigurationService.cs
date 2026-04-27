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

    public RuleConfigurationService(
        IOptions<EmptySectionStructureRuleOptions> emptySectionOptions,
        IOptions<FontFamilyRuleOptions>? fontFamilyOptions = null)
    {
        _emptySectionOptions = emptySectionOptions.Value;
        _fontFamilyOptions = fontFamilyOptions?.Value ?? new FontFamilyRuleOptions();
    }

    public bool IsRuleAvailable(string ruleId)
    {
        if (IsEmptySectionStructureRule(ruleId))
            return _emptySectionOptions.Availability != RuleAvailability.Hidden;

        if (IsFontFamilyRule(ruleId))
            return _fontFamilyOptions.Availability != RuleAvailability.Hidden;

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

        return SeverityResolver.Resolve(ruleId, config, explicitSeverity);
    }

    public RuleDefinition ApplyConfiguration(RuleDefinition definition)
    {
        if (IsEmptySectionStructureRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_emptySectionOptions.Severity.ToString()) };

        if (IsFontFamilyRule(definition.Id))
            return definition with { DefaultSeverity = ValidationSeverity.Normalize(_fontFamilyOptions.Severity.ToString()) };

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
}
