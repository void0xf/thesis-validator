namespace ThesisValidator.Rules;

public interface IValidationRule
{
    RuleDescriptor Descriptor { get; }

    Type OptionsType { get; }

    IEnumerable<RuleProblem> Validate(
        RuleContext context,
        object options);
}
