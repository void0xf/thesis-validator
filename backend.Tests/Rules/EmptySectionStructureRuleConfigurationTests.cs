using System.Collections;
using System.Reflection;
using backend.Endpoints;
using backend.Models;
using backend.Rules;
using backend.RuleOptions;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Rules;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class EmptySectionStructureRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenEmptySectionRuleIsAvailable_IncludesRule()
    {
        var result = InvokeGetAvailableRules(new EmptySectionStructureRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        Assert.Contains(EmptySectionStructureRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenEmptySectionSeverityIsConfigured_ReportsConfiguredSeverity()
    {
        var result = InvokeGetAvailableRules(new EmptySectionStructureRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, GetRuleDefaultSeverity(result, EmptySectionStructureRule.RuleId));
    }

    [Fact]
    public void GetAvailableRules_WhenEmptySectionRuleIsHidden_ExcludesRule()
    {
        var result = InvokeGetAvailableRules(new EmptySectionStructureRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        Assert.DoesNotContain(EmptySectionStructureRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(EmptySectionStructureRule.RuleId);
        var service = CreateService(
            [rule],
            new EmptySectionStructureRuleOptions
            {
                Availability = RuleAvailability.Hidden
            });

        using var stream = CreateEmptySectionDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [EmptySectionStructureRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateEmptySectionRule(new EmptySectionStructureRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Warning
        });

        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.False(result.IsError);
    }

    [Fact]
    public void Validate_WhenSeverityIsError_AppliesError()
    {
        var result = ValidateEmptySectionRule(new EmptySectionStructureRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WhenConfiguredSeverityMatchesPreviousDefault_KeepsExistingErrorBehavior()
    {
        Assert.Equal(
            ValidationSeverity.Error,
            RuleCatalog.GetDefinition(EmptySectionStructureRule.RuleId).DefaultSeverity);

        var result = ValidateEmptySectionRule(new EmptySectionStructureRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
    }

    private static ValidationResult ValidateEmptySectionRule(EmptySectionStructureRuleOptions options)
    {
        var rule = new EmptySectionStructureRule(CreateRuleConfigurationService(options));
        using var stream = CreateEmptySectionDocxStream();
        using var doc = WordprocessingDocument.Open(stream, false);

        return Assert.Single(rule.Validate(doc, new UniversityConfig()));
    }

    private static IResult InvokeGetAvailableRules(EmptySectionStructureRuleOptions options)
    {
        var method = typeof(DocumentEndpoint).GetMethod(
            "GetAvailableRules",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(
            null,
            new object?[]
            {
                CreateService(
                    [new RecordingRule(EmptySectionStructureRule.RuleId), new RecordingRule("FontFamily")],
                    options),
                CreateRuleConfigurationService(options)
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static ThesisValidatorService CreateService(
        IEnumerable<IValidationRule> rules,
        EmptySectionStructureRuleOptions options)
    {
        return new ThesisValidatorService(rules, CreateRuleConfigurationService(options));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        EmptySectionStructureRuleOptions options)
    {
        return new RuleConfigurationService(Options.Create(options));
    }

    private static IReadOnlyList<string> GetRuleNames(IResult result)
    {
        return GetRules(result)
            .Select(rule => rule.GetType().GetProperty("Name")?.GetValue(rule) as string)
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();
    }

    private static string? GetRuleDefaultSeverity(IResult result, string ruleName)
    {
        return GetRules(result)
            .Where(rule => string.Equals(
                rule.GetType().GetProperty("Name")?.GetValue(rule) as string,
                ruleName,
                StringComparison.OrdinalIgnoreCase))
            .Select(rule => rule.GetType().GetProperty("DefaultSeverity")?.GetValue(rule) as string)
            .SingleOrDefault();
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

    private sealed class RecordingRule : IValidationRule
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
}
