using Microsoft.Extensions.Configuration;

namespace ThesisValidator.Rules;

public sealed class RulePolicyResolver
{
    private readonly IConfiguration _configuration;

    public RulePolicyResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public RulePolicy Resolve(RuleDescriptor descriptor)
    {
        var section = _configuration.GetSection(
            $"Validation:Rules:{descriptor.Name}");

        var configuredPolicy = section.Get<RulePolicyOptions>();

        return new RulePolicy(
            Availability: configuredPolicy?.Availability
                ?? descriptor.DefaultAvailability,
            Severity: configuredPolicy?.Severity
                ?? descriptor.DefaultSeverity);
    }
}
