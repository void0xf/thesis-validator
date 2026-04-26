using backend.Models;
using Backend.Models;
using ThesisValidator.Rules;

namespace backend.Services.Results;

public static class SeverityResolver
{
    public static string Resolve(
        string ruleId,
        UniversityConfig config,
        string? explicitSeverity = null)
    {
        var configuredSeverity = config.Rules.GetOverride(ruleId)?.Severity;
        if (!string.IsNullOrWhiteSpace(configuredSeverity))
            return ValidationSeverity.Normalize(configuredSeverity);

        if (!string.IsNullOrWhiteSpace(explicitSeverity))
            return ValidationSeverity.Normalize(explicitSeverity);

        return ValidationSeverity.Normalize(RuleCatalog.GetDefinition(ruleId).DefaultSeverity);
    }

    public static bool IsRuleEnabled(string ruleId, UniversityConfig config)
    {
        return config.Rules.GetOverride(ruleId)?.Enabled
            ?? RuleCatalog.GetDefinition(ruleId).Enabled;
    }
}
