using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Content;
using backend.Application.Validation;
using backend.Annotation;
using backend.Models;
using backend.Rules;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class EmptySectionStructureRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenEmptySectionRuleIsAvailable_IncludesRule()
    {
        var service = CreateService();

        var rules = service.GetAvailableRules();

        Assert.Contains(rules, rule => rule.Id == EmptySectionStructureRule.RuleId);
    }

    [Fact]
    public void GetAvailableRules_WhenEmptySectionRuleIsHidden_ExcludesRule()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Validation:Rules:EmptySectionStructureRule:Availability"] = "Hidden"
        });

        var rules = service.GetAvailableRules();

        Assert.DoesNotContain(rules, rule => rule.Id == EmptySectionStructureRule.RuleId);
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Validation:Rules:EmptySectionStructureRule:Availability"] = "Hidden"
        });
        using var stream = CreateEmptySectionDocxStream();

        var results = service.Validate(stream, [EmptySectionStructureRule.RuleId]);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateEmptySectionRule(new Dictionary<string, string?>
        {
            ["Validation:Rules:EmptySectionStructureRule:Severity"] = "Warning"
        });

        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.False(result.IsError);
    }

    [Fact]
    public void Validate_WhenSeverityIsError_AppliesError()
    {
        var result = ValidateEmptySectionRule(new Dictionary<string, string?>
        {
            ["Validation:Rules:EmptySectionStructureRule:Severity"] = "Error"
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_StillPopulatesSectionContext()
    {
        var result = ValidateEmptySectionRule();

        Assert.Equal("Chapter 1", result.Location.Section);
    }

    private static ValidationResult ValidateEmptySectionRule(
        Dictionary<string, string?>? configurationValues = null)
    {
        var service = CreateService(configurationValues);
        using var stream = CreateEmptySectionDocxStream();

        return Assert.Single(service.Validate(stream, [EmptySectionStructureRule.RuleId]));
    }

    private static ThesisValidationOrchestrator CreateService(
        Dictionary<string, string?>? configurationValues = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues ?? new Dictionary<string, string?>())
            .Build();
        var policyResolver = new RulePolicyResolver(configuration);
        var optionsBinder = new RuleOptionsBinder(configuration);
        var resultComposer = new ValidationResultComposer();

        return new ThesisValidationOrchestrator(
            new DocumentSession(),
            new DocumentContentAnalyzer(new DocumentSkipResolver(
                Options.Create(new ValidationSkippingOptions()))),
            new RuleRunner(
                [new EmptySectionStructureRule()],
                policyResolver,
                optionsBinder,
                resultComposer),
            new SectionContextResolver(),
            new AnnotationApplicator());
    }

    private static MemoryStream CreateEmptySectionDocxStream()
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    CreateHeading("Chapter 1", "Heading1"),
                    CreateHeading("Section 1.1", "Heading2")));
            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static Paragraph CreateHeading(string text, string styleId)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
            new Run(new Text(text)));
    }
}
