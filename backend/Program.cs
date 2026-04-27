using System.Reflection;
using backend.Endpoints;
using backend.Models;
using backend.RuleOptions;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Language;
using Backend.Models;
using ThesisValidator.Rules;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<UniversityConfig>(
    builder.Configuration.GetSection("UniversityConfig"));

builder.Services.AddOptions<CodeBlockDetectionOptions>()
    .Bind(builder.Configuration.GetSection(CodeBlockDetectionOptions.SectionName))
    .Validate(
        options => options.MinimumCodeFontTextRatio > 0 && options.MinimumCodeFontTextRatio <= 1,
        "CodeBlockDetection:MinimumCodeFontTextRatio must be greater than 0 and less than or equal to 1.")
    .Validate(
        options => options.CodeFonts is not null
            && options.CodeFonts.Any(font => !string.IsNullOrWhiteSpace(font)),
        "CodeBlockDetection:CodeFonts must contain at least one font.")
    .ValidateOnStart();

builder.Services.AddOptions<EmptySectionStructureRuleOptions>()
    .Bind(builder.Configuration.GetSection(EmptySectionStructureRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<ICodeBlockDetector, CodeBlockDetector>();

builder.Services.AddHttpClient<LanguageToolService>();
builder.Services.AddScoped<LanguageToolService>();

var assembly = typeof(Program).Assembly;
var ruleTypes = assembly.
    GetTypes().
    Where(t => typeof(IValidationRule).IsAssignableFrom(t)
                                              && !t.IsInterface
                                              && !t.IsAbstract);
foreach (var ruleType in ruleTypes)
{
    builder.Services.AddScoped(typeof(IValidationRule), ruleType);
}

builder.Services.AddScoped<ThesisValidatorService>();
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
