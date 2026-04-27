using Backend.Models;
using ThesisValidator.Rules;

namespace backend.Services.Rules;

public interface IRuleConfigurationService
{
    bool IsRuleAvailable(string ruleId);

    string ResolveSeverity(
        string ruleId,
        UniversityConfig config,
        string? explicitSeverity = null);

    RuleDefinition ApplyConfiguration(RuleDefinition definition);
}
