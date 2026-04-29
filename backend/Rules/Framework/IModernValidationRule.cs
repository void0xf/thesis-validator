namespace ThesisValidator.Rules;

public interface IModernValidationRule
{
    RuleDescriptor Descriptor { get; }

    Type OptionsType { get; }

    IEnumerable<RuleProblem> Validate(
        RuleContext context,
        object options);
}
