using System.Collections;
using System.Reflection;
using backend.Endpoints;
using backend.Models;
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
using Rules;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class HierarchyDepthRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenHierarchyDepthRuleIsAvailable_IncludesRule()
    {
        var result = InvokeGetAvailableRules(new HierarchyDepthRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        Assert.Contains(HierarchyDepthRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenHierarchyDepthRuleIsHidden_ExcludesRule()
    {
        var result = InvokeGetAvailableRules(new HierarchyDepthRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        Assert.DoesNotContain(HierarchyDepthRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(HierarchyDepthRule.RuleId);
        var service = CreateService(
            [rule],
            new HierarchyDepthRuleOptions
            {
                Availability = RuleAvailability.Hidden
            });

        using var stream = CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [HierarchyDepthRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateHierarchyDepthRule(new HierarchyDepthRuleOptions
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
        var result = ValidateHierarchyDepthRule(new HierarchyDepthRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsHierarchyDepthWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(HierarchyDepthRule.RuleId);
        var unselectedRule = new RecordingRule("Grammar");
        var service = CreateService(
            [selectedRule, unselectedRule],
            new HierarchyDepthRuleOptions
            {
                Availability = RuleAvailability.Available
            });

        using var stream = CreateDocxStream();
        service.Validate(stream, new UniversityConfig(), [HierarchyDepthRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    [Fact]
    public void Validate_WhenMaxAllowedLevelIsConfigured_UsesConfiguredLevel()
    {
        var options = new HierarchyDepthRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error,
            MaxAllowedLevel = 4
        };
        var rule = CreateRule(options);
        using var docx = CreateHeadingDocx("Heading4");

        var results = rule.Validate(docx, new UniversityConfig()).ToList();

        Assert.Empty(results);
    }

    private static ValidationResult ValidateHierarchyDepthRule(HierarchyDepthRuleOptions options)
    {
        var rule = CreateRule(options);
        using var docx = CreateHeadingDocx("Heading4");

        return Assert.Single(rule.Validate(docx, new UniversityConfig()));
    }

    private static HierarchyDepthRule CreateRule(HierarchyDepthRuleOptions options)
    {
        return new HierarchyDepthRule(
            CreateRuleConfigurationService(options),
            Options.Create(options));
    }

    private static IResult InvokeGetAvailableRules(HierarchyDepthRuleOptions options)
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
                    [new RecordingRule(HierarchyDepthRule.RuleId), new RecordingRule("Grammar")],
                    options),
                CreateRuleConfigurationService(options)
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static ThesisValidatorService CreateService(
        IEnumerable<IValidationRule> rules,
        HierarchyDepthRuleOptions options)
    {
        return new ThesisValidatorService(rules, CreateRuleConfigurationService(options));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        HierarchyDepthRuleOptions options)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            hierarchyDepthOptions: Options.Create(options));
    }

    private static MemoryStream CreateDocxStream()
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("Body text")))));
            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static WordprocessingDocument CreateHeadingDocx(string styleId)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(
            new Body(
                new Paragraph(
                    new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
                    new Run(new Text("Too deep heading")))));
        mainPart.Document.Save();
        stream.Position = 0;
        return doc;
    }

    private static IReadOnlyList<string> GetRuleNames(IResult result)
    {
        return GetRules(result)
            .Select(rule => rule.GetType().GetProperty("Name")?.GetValue(rule) as string)
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();
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
