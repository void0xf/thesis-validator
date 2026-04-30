
namespace ThesisValidator.Rules;

public abstract class ValidationRule<TOptions> : IValidationRule
    where TOptions : class, new()
{
    public abstract RuleDescriptor Descriptor { get; }

    public Type OptionsType => typeof(TOptions);

    public abstract IEnumerable<RuleProblem> Validate(
            RuleContext context,
            TOptions options);

    IEnumerable<RuleProblem> IValidationRule.Validate(
        RuleContext context,
        object options)
    {
        if (options is not TOptions typedOptions)
        {
            throw new InvalidOperationException(
                $"Invalid options type for rule '{Descriptor.Name}'. " +
                $"Expected {typeof(TOptions).Name}, got {options.GetType().Name}.");
        }

        return Validate(context, typedOptions);
    }


}
