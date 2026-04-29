using Microsoft.Extensions.Configuration;

namespace ThesisValidator.Rules;

public sealed class RuleOptionsBinder
{
    private readonly IConfiguration _configuration;

    public RuleOptionsBinder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public object Bind(IModernValidationRule rule)
    {
        var options = Activator.CreateInstance(rule.OptionsType)
            ?? throw new InvalidOperationException(
                $"Could not create options for rule '{rule.Descriptor.Name}'.");

        var section = _configuration.GetSection(
            $"Validation:Rules:{rule.Descriptor.Name}");

        section.Bind(options);

        return options;
    }
}
