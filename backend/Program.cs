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

builder.Services.AddOptions<NoDotsInTitlesRuleOptions>()
    .Bind(builder.Configuration.GetSection(NoDotsInTitlesRuleOptions.SectionName))
    .Validate(options => options.TargetStylePatterns is not null
            && options.TargetStylePatterns.Any(pattern => !string.IsNullOrWhiteSpace(pattern)),
        "NoDotsInTitlesRule:TargetStylePatterns must contain at least one non-empty style pattern.")
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

builder.Services.AddOptions<ParagraphIndentRuleOptions>()
    .Bind(builder.Configuration.GetSection(ParagraphIndentRuleOptions.SectionName))
    .Validate(options => options.AllowedIndentTwips is not null
            && options.AllowedIndentTwips.Length > 0
            && options.AllowedIndentTwips.All(indent => indent >= 0),
        "RequiredIndentCm:AllowedIndentTwips must contain only non-negative values and at least one value.")
    .Validate(options => options.ToleranceTwips >= 0,
        "RequiredIndentCm:ToleranceTwips must be greater than or equal to 0.")
    .ValidateOnStart();

builder.Services.AddOptions<SingleSpaceRuleOptions>()
    .Bind(builder.Configuration.GetSection(SingleSpaceRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<TextJustificationRuleOptions>()
    .Bind(builder.Configuration.GetSection(TextJustificationRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<TocRuleOptions>()
    .Bind(builder.Configuration.GetSection(TocRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<ManualTableOfContentsRuleOptions>()
    .Bind(builder.Configuration.GetSection(ManualTableOfContentsRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<MissingFigureCaptionRuleOptions>()
    .Bind(builder.Configuration.GetSection(MissingFigureCaptionRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<FigureCaptionPositionRuleOptions>()
    .Bind(builder.Configuration.GetSection(FigureCaptionPositionRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<FigureCaptionFormatRuleOptions>()
    .Bind(builder.Configuration.GetSection(FigureCaptionFormatRuleOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<GrammarRuleOptions>()
    .Bind(builder.Configuration.GetSection(GrammarRuleOptions.SectionName))
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
