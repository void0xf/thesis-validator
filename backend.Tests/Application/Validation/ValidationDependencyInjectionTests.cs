using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using backend.Annotation;
using backend.Application.Validation;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Paragraphs;
using backend.Infrastructure.LanguageTool;
using backend.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThesisValidator.Rules;

namespace backend.Tests.Application.Validation;

public sealed class ValidationDependencyInjectionTests
{
    [Fact]
    public void ServiceProvider_CanResolveValidatorAndValidationRules()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LanguageTool:BaseUrl"] = "http://localhost:8010",
                ["Validation:Skipping:SkipTextBoxes"] = "true",
                ["Validation:Skipping:SkipTableOfContentsContent"] = "true"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpClient<LanguageToolClient>();
        services.AddOptions<ValidationSkippingOptions>()
            .Bind(configuration.GetSection(ValidationSkippingOptions.SectionName));

        services.AddSingleton<ValidationResultComposer>();
        services.AddSingleton<RulePolicyResolver>();
        services.AddSingleton<RuleOptionsBinder>();
        services.AddSingleton<DocumentSession>();
        services.AddSingleton<DocumentSkipResolver>();
        services.AddSingleton<DocumentContentAnalyzer>();
        services.AddSingleton<FormattingResolver>();
        services.AddSingleton<ParagraphClassifier>();
        services.AddSingleton<ListAnalyzer>();
        services.AddSingleton<FigureCaptionAnalyzer>();
        services.AddScoped<RuleRunner>();
        services.AddSingleton<SectionContextResolver>();
        services.AddSingleton<AnnotationApplicator>();

        var validationRuleTypes = typeof(FontFamilyRule).Assembly.GetTypes()
            .Where(t => typeof(IValidationRule).IsAssignableFrom(t)
                && !t.IsInterface
                && !t.IsAbstract
                && !t.IsNested);

        foreach (var ruleType in validationRuleTypes)
        {
            services.AddScoped(typeof(IValidationRule), ruleType);
        }

        services.AddScoped<ThesisValidationOrchestrator>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        var validator = scope.ServiceProvider.GetRequiredService<ThesisValidationOrchestrator>();
        var ruleIds = validator.GetAvailableRules()
            .Select(rule => rule.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(FontFamilyRule.RuleId, ruleIds);
        Assert.Contains(GrammarRule.RuleId, ruleIds);
    }
}
