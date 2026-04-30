using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using backend.DocumentProcessing.Paragraphs;
using backend.DocumentProcessing.Lists;
using backend.DocumentProcessing.Formatting;
using backend.DocumentProcessing.Figures;
using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Content;
using backend.Application.Validation;
using backend.Annotation;
using System.Reflection;
using backend.Endpoints;
using backend.Infrastructure.LanguageTool;
using ThesisValidator.Rules;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<LanguageToolClient>();

builder.Services.AddOptions<ValidationSkippingOptions>()
    .Bind(builder.Configuration.GetSection(ValidationSkippingOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<ValidationResultComposer>();
builder.Services.AddSingleton<RulePolicyResolver>();
builder.Services.AddSingleton<RuleOptionsBinder>();
builder.Services.AddSingleton<DocumentSession>();
builder.Services.AddSingleton<DocumentSkipResolver>();
builder.Services.AddSingleton<DocumentContentAnalyzer>();
builder.Services.AddSingleton<FormattingResolver>();
builder.Services.AddSingleton<ParagraphClassifier>();
builder.Services.AddSingleton<ListAnalyzer>();
builder.Services.AddSingleton<FigureCaptionAnalyzer>();
builder.Services.AddScoped<RuleRunner>();
builder.Services.AddSingleton<SectionContextResolver>();
builder.Services.AddSingleton<AnnotationApplicator>();


var validationRuleTypes = typeof(Program).Assembly.GetTypes()
    .Where(t => typeof(IValidationRule).IsAssignableFrom(t)
        && !t.IsInterface
        && !t.IsAbstract
        && !t.IsNested);

foreach (var ruleType in validationRuleTypes)
{
    builder.Services.AddScoped(typeof(IValidationRule), ruleType);
}

builder.Services.AddScoped<ThesisValidationOrchestrator>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapDocumentEndpoint();

app.UseHttpsRedirection();

app.Run();
