using System.Collections;
using System.Reflection;
using backend.Endpoints;
using backend.Models;
using backend.ModernServices;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Rules;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

internal static class RuleConfigurationTestSupport
{
    public static IResult InvokeGetAvailableRules(
        IEnumerable<IValidationRule> rules,
        IRuleConfigurationService ruleConfigurationService)
    {
        var method = typeof(DocumentEndpoint).GetMethod(
            "GetAvailableRules",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(
            null,
            new object?[]
            {
                CreateModernValidator(rules)
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    public static IReadOnlyList<string> GetRuleNames(IResult result)
    {
        return GetRules(result)
            .Select(rule => rule.GetType().GetProperty("Name")?.GetValue(rule) as string)
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();
    }

    public static MemoryStream CreateDocxStream()
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("Body text")))));
            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static IEnumerable<object> GetRules(IResult result)
    {
        Assert.Equal(StatusCodes.Status200OK, result.GetType().GetProperty("StatusCode")?.GetValue(result));

        var value = result.GetType().GetProperty("Value")?.GetValue(result);
        Assert.NotNull(value);

        var rules = value.GetType().GetProperty("Rules")?.GetValue(value);
        Assert.NotNull(rules);

        return ((IEnumerable)rules).Cast<object>();
    }

    private static ModernThesisValidatorService CreateModernValidator(IEnumerable<IValidationRule> rules)
    {
        var configuration = new ConfigurationBuilder().Build();
        var policyResolver = new RulePolicyResolver(configuration);
        var optionsBinder = new RuleOptionsBinder(configuration);
        var resultComposer = new ValidationResultComposer();
        var modernRules = rules.Select(rule => new LegacyRuleDescriptorAdapter(rule.Name)).ToList();

        return new ModernThesisValidatorService(
            new ModernDocumentSession(),
            new backend.ModernServices.DocumentContentAnalyzer(new ModernDocumentSkipService(
                Options.Create(new ModernValidationOptions()))),
            new ModernRuleRunner(modernRules, policyResolver, optionsBinder, resultComposer),
            new ModernSectionContextService(),
            new ModernAnnotationApplier());
    }

    private sealed class LegacyRuleDescriptorAdapter : ValidationRule<NoRuleOptions>
    {
        private readonly string _name;

        public LegacyRuleDescriptorAdapter(string name)
        {
            _name = name;
        }

        public override RuleDescriptor Descriptor => new(
            Name: _name,
            DisplayName: _name,
            Description: _name,
            Category: RuleCategories.Formatting,
            DefaultAvailability: backend.RuleOptions.RuleAvailability.Available,
            DefaultSeverity: backend.RuleOptions.RuleSeverity.Error);

        public override IEnumerable<RuleProblem> Validate(
            RuleContext context,
            NoRuleOptions options)
        {
            return [];
        }
    }
}

internal sealed class RecordingRule : IValidationRule
{
    public RecordingRule(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public int RunCount { get; private set; }

    public IEnumerable<ValidationResult> Validate(
        WordprocessingDocument doc,
        UniversityConfig config,
        DocumentCommentService? documentCommentService = null)
    {
        RunCount++;
        return
        [
            new ValidationResult
            {
                RuleName = Name,
                Message = "Executed"
            }
        ];
    }
}
