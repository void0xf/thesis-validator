using ThesisValidator.Rules;

namespace backend.Application.Validation;

public sealed class RuleRunner
{
    private readonly IReadOnlyList<IValidationRule> _rules;
    private readonly RulePolicyResolver _policyResolver;
    private readonly RuleOptionsBinder _optionsBinder;
    private readonly ValidationIssueComposer _resultComposer;

    public RuleRunner(
        IEnumerable<IValidationRule> rules,
        RulePolicyResolver policyResolver,
        RuleOptionsBinder optionsBinder,
        ValidationIssueComposer resultComposer)
    {
        _rules = rules.ToList();
        _policyResolver = policyResolver;
        _optionsBinder = optionsBinder;
        _resultComposer = resultComposer;
    }

    public IReadOnlyList<AvailableValidationRule> GetAvailableRules()
    {
        return _rules
            .Select(rule => (Rule: rule, Policy: _policyResolver.Resolve(rule.Descriptor)))
            .Where(pair => pair.Policy.Availability != RuleAvailability.Hidden)
            .Select(pair => new AvailableValidationRule(
                pair.Rule.Descriptor.Name,
                pair.Rule.Descriptor.DisplayName,
                pair.Rule.Descriptor.Category,
                pair.Policy.Severity.ToString()))
            .ToList();
    }

    public IReadOnlyList<string> GetUnknownRuleNames(IEnumerable<string> selectedRules)
    {
        var knownRules = _rules
            .Where(rule => _policyResolver.Resolve(rule.Descriptor).Availability != RuleAvailability.Hidden)
            .Select(rule => rule.Descriptor.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return selectedRules
            .Where(ruleName => !knownRules.Contains(ruleName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<RuleExecution> Run(
        RuleContext context,
        IEnumerable<string>? selectedRules)
    {
        var executions = new List<RuleExecution>();

        foreach (var rule in GetRulesToRun(selectedRules))
        {
            var policy = _policyResolver.Resolve(rule.Descriptor);
            var boundOptions = _optionsBinder.Bind(rule);

            foreach (var problem in rule.Validate(context, boundOptions))
            {
                var result = _resultComposer.Compose(
                    rule.Descriptor,
                    policy,
                    problem);

                executions.Add(new RuleExecution(result, problem));
            }
        }

        return executions;
    }

    private IReadOnlyList<IValidationRule> GetRulesToRun(IEnumerable<string>? selectedRules)
    {
        var selectedSet = selectedRules?
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _rules
            .Where(rule =>
            {
                var policy = _policyResolver.Resolve(rule.Descriptor);
                if (policy.Availability == RuleAvailability.Hidden)
                    return false;

                return selectedSet is null
                    || selectedSet.Count == 0
                    || selectedSet.Contains(rule.Descriptor.Name);
            })
            .ToList();
    }
}
