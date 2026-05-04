using System.Reflection;
using backend.Annotation;
using backend.Application.Validation;
using backend.DocumentProcessing.CodeBlocks;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Paragraphs;
using backend.Infrastructure.LanguageTool;
using ThesisValidator.Rules;
using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;

namespace backend.Extensions;

internal static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "AllowFrontend";

    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();

        var allowedOrigins = GetAllowedOrigins(configuration, environment);

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static string[] GetAllowedOrigins(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray();

        if (allowedOrigins is { Length: > 0 })
        {
            return allowedOrigins;
        }

        if (environment.IsDevelopment())
        {
            return ["http://localhost:4200"];
        }

        throw new InvalidOperationException(
            "Cors:AllowedOrigins must be configured outside the Development environment.");
    }

    public static IServiceCollection AddDocumentValidation(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly rulesAssembly)
    {
        services.AddHttpClient<LanguageToolClient>();

        services.AddOptions<ValidationSkippingOptions>()
            .Bind(configuration.GetSection(ValidationSkippingOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<CodeBlockDetectionOptions>()
            .Bind(configuration.GetSection(CodeBlockDetectionOptions.SectionName))
            .Validate(
                options => options.MinimumCodeFontTextRatio is >= 0 and <= 1,
                "Code block font text ratio must be between 0 and 1.")
            .ValidateOnStart();

        services.AddSingleton<ValidationIssueComposer>();
        services.AddSingleton<RulePolicyResolver>();
        services.AddSingleton<RuleOptionsBinder>();
        services.AddSingleton<DocumentSession>();
        services.AddSingleton<DocumentSkipResolver>();
        services.AddSingleton<ICodeBlockDetector, CodeBlockDetector>();
        services.AddSingleton<DocumentContentAnalyzer>();
        services.AddSingleton<FormattingResolver>();
        services.AddSingleton<ParagraphClassifier>();
        services.AddSingleton<ListAnalyzer>();
        services.AddSingleton<FigureCaptionAnalyzer>();
        services.AddSingleton<SectionContextResolver>();
        services.AddSingleton<AnnotationApplicator>();

        services.AddScoped<RuleRunner>();
        services.AddScoped<ThesisValidationOrchestrator>();
        services.AddValidationRulesFrom(rulesAssembly);

        return services;
    }

    private static IServiceCollection AddValidationRulesFrom(
        this IServiceCollection services,
        Assembly assembly)
    {
        var validationRuleTypes = assembly.GetTypes()
            .Where(type => typeof(IValidationRule).IsAssignableFrom(type)
                && !type.IsInterface
                && !type.IsAbstract
                && !type.IsNested);

        foreach (var ruleType in validationRuleTypes)
        {
            services.AddScoped(typeof(IValidationRule), ruleType);
        }

        return services;
    }
}
