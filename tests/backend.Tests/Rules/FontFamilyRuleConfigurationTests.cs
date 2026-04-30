using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Content;
using backend.Application.Validation;
using backend.Annotation;
using backend.Rules;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class FontFamilyRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenFontFamilyRuleIsAvailable_IncludesRule()
    {
        var service = CreateService();

        var rules = service.GetAvailableRules();

        Assert.Contains(rules, rule => rule.Id == FontFamilyRule.RuleId);
    }

    [Fact]
    public void GetAvailableRules_WhenFontFamilyRuleIsHidden_ExcludesRule()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            [$"Validation:Rules:{FontFamilyRule.RuleId}:Availability"] = "Hidden"
        });

        var rules = service.GetAvailableRules();

        Assert.DoesNotContain(rules, rule => rule.Id == FontFamilyRule.RuleId);
    }

    [Fact]
    public void GetAvailableRules_WhenSeverityIsWarning_ReportsWarning()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            [$"Validation:Rules:{FontFamilyRule.RuleId}:Severity"] = "Warning"
        });

        var rule = Assert.Single(
            service.GetAvailableRules(),
            rule => rule.Id == FontFamilyRule.RuleId);

        Assert.Equal(RuleSeverity.Warning.ToString(), rule.DefaultSeverity);
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            [$"Validation:Rules:{FontFamilyRule.RuleId}:Availability"] = "Hidden"
        });
        using var stream = CreateDocxStream(("Wrong font paragraph", "Arial"));

        var results = service.Validate(stream, [FontFamilyRule.RuleId]);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateFontFamilyRule(new Dictionary<string, string?>
        {
            [$"Validation:Rules:{FontFamilyRule.RuleId}:Severity"] = "Warning"
        });

        Assert.Equal(RuleSeverity.Warning, result.Severity);
        Assert.False(result.IsError);
    }

    [Fact]
    public void Validate_WhenSeverityIsError_AppliesError()
    {
        var result = ValidateFontFamilyRule(new Dictionary<string, string?>
        {
            [$"Validation:Rules:{FontFamilyRule.RuleId}:Severity"] = "Error"
        });

        Assert.Equal(RuleSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WhenRequiredFontFamilyIsConfigured_UsesConfiguredFont()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            [$"Validation:Rules:{FontFamilyRule.RuleId}:RequiredFontFamily"] = "Arial"
        });
        using var stream = CreateDocxStream(("Configured font paragraph", "Arial"));

        var results = service.Validate(stream, [FontFamilyRule.RuleId]);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsFontFamilyWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(FontFamilyRule.RuleId);
        var unselectedRule = new RecordingRule("Grammar");
        var service = CreateService(
            configurationValues: null,
            rules: [selectedRule, unselectedRule]);
        using var stream = CreateDocxStream(("Body text", "Times New Roman"));

        service.Validate(stream, [FontFamilyRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    private static ValidationIssue ValidateFontFamilyRule(
        Dictionary<string, string?>? configurationValues = null)
    {
        var service = CreateService(configurationValues);
        using var stream = CreateDocxStream(("Wrong font paragraph", "Arial"));

        return Assert.Single(service.Validate(stream, [FontFamilyRule.RuleId]));
    }

    private static ThesisValidationOrchestrator CreateService(
        Dictionary<string, string?>? configurationValues = null,
        IEnumerable<IValidationRule>? rules = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues ?? new Dictionary<string, string?>())
            .Build();
        var policyResolver = new RulePolicyResolver(configuration);
        var optionsBinder = new RuleOptionsBinder(configuration);
        var resultComposer = new ValidationIssueComposer();

        return new ThesisValidationOrchestrator(
            new DocumentSession(),
            new DocumentContentAnalyzer(new DocumentSkipResolver(
                Options.Create(new ValidationSkippingOptions()))),
            new RuleRunner(
                rules ?? [new FontFamilyRule()],
                policyResolver,
                optionsBinder,
                resultComposer),
            new SectionContextResolver(),
            new AnnotationApplicator());
    }

    private static MemoryStream CreateDocxStream(params (string Text, string? FontFamily)[] paragraphs)
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            foreach (var (text, fontFamily) in paragraphs)
            {
                var run = new Run(new Text(text));
                if (!string.IsNullOrWhiteSpace(fontFamily))
                {
                    run.RunProperties = new RunProperties(
                        new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily });
                }

                mainPart.Document.Body!.Append(new Paragraph(run));
            }

            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class RecordingRule : ValidationRule<RecordingRuleOptions>
    {
        private readonly string _name;

        public RecordingRule(string name)
        {
            _name = name;
        }

        public int RunCount { get; private set; }

        public override RuleDescriptor Descriptor => new(
            Name: _name,
            DisplayName: _name,
            Description: _name,
            Category: RuleCategories.Formatting,
            DefaultAvailability: RuleAvailability.Available,
            DefaultSeverity: RuleSeverity.Error);

        public override IEnumerable<RuleProblem> Validate(
            RuleContext context,
            RecordingRuleOptions options)
        {
            RunCount++;

            yield return new RuleProblem(
                "Executed",
                new DocumentLocation(),
                ParagraphIndexKind.BodyElement);
        }
    }

    private sealed class RecordingRuleOptions : RuleOptionsBase;
}
