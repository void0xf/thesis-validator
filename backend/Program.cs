using System.Reflection;
using backend.Endpoints;
using backend.Models;
using backend.RuleOptions;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Language;
using backend.Services.Rules;
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

builder.Services.AddOptions<FontFamilyRuleOptions>()
    .Bind(builder.Configuration.GetSection(FontFamilyRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<HeadingStyleUsageRuleOptions>()
    .Bind(builder.Configuration.GetSection(HeadingStyleUsageRuleOptions.SectionName))
    .Validate(options => options.FontSizeThresholdAboveBodyPt >= 0,
        "HeadingStyleUsageRule:FontSizeThresholdAboveBodyPt must be greater than or equal to 0.")
    .Validate(options => options.MaxHeadingTextLength > 0,
        "HeadingStyleUsageRule:MaxHeadingTextLength must be greater than 0.")
    .ValidateOnStart();

builder.Services.AddOptions<HierarchyDepthRuleOptions>()
    .Bind(builder.Configuration.GetSection(HierarchyDepthRuleOptions.SectionName))
    .Validate(options => options.MaxAllowedLevel > 0,
        "HierarchyDepthRule:MaxAllowedLevel must be greater than 0.")
    .ValidateOnStart();

builder.Services.AddOptions<LineSpacingDependencyRuleOptions>()
    .Bind(builder.Configuration.GetSection(LineSpacingDependencyRuleOptions.SectionName))
    .Validate(options => options.TargetLineSpacingTwips > 0,
        "LineSpacingDependencyRule:TargetLineSpacingTwips must be greater than 0.")
    .ValidateOnStart();

builder.Services.AddOptions<MissingFigureCaptionRuleOptions>()
    .Bind(builder.Configuration.GetSection(MissingFigureCaptionRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<ListPunctuationConsistencyRuleOptions>()
    .Bind(builder.Configuration.GetSection(ListPunctuationConsistencyRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<ListIndentationConsistencyRuleOptions>()
    .Bind(builder.Configuration.GetSection(ListIndentationConsistencyRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddScoped<IRuleConfigurationService, RuleConfigurationService>();

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
