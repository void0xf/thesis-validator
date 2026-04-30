using System.Reflection;
using backend.Endpoints;
using backend.ModernServices;
using backend.RuleOptions;
using backend.ModernServices.Language;
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

builder.Services.AddHttpClient<LanguageToolService>();
builder.Services.AddScoped<LanguageToolService>();

builder.Services.AddOptions<ModernValidationOptions>()
    .Bind(builder.Configuration.GetSection(ModernValidationOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<ValidationResultComposer>();
builder.Services.AddSingleton<RulePolicyResolver>();
builder.Services.AddSingleton<RuleOptionsBinder>();
builder.Services.AddSingleton<ModernDocumentSession>();
builder.Services.AddSingleton<ModernDocumentSkipService>();
builder.Services.AddSingleton<DocumentContentAnalyzer>();
builder.Services.AddSingleton<ModernFormattingResolver>();
builder.Services.AddSingleton<ModernParagraphClassifier>();
builder.Services.AddSingleton<ModernListAnalyzer>();
builder.Services.AddSingleton<ModernFigureCaptionAnalyzer>();
builder.Services.AddScoped<ModernRuleRunner>();
builder.Services.AddSingleton<ModernSectionContextService>();
builder.Services.AddSingleton<ModernAnnotationApplier>();


var modernRuleTypes = typeof(Program).Assembly.GetTypes()
    .Where(t => typeof(IModernValidationRule).IsAssignableFrom(t)
        && !t.IsInterface
        && !t.IsAbstract
        && !t.IsNested);

foreach (var ruleType in modernRuleTypes)
{
    builder.Services.AddScoped(typeof(IModernValidationRule), ruleType);
}

builder.Services.AddScoped<ModernThesisValidatorService>();
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
