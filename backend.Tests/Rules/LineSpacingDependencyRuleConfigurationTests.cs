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

public class LineSpacingDependencyRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenLineSpacingDependencyRuleIsAvailable_IncludesRule()
    {
        var result = InvokeGetAvailableRules(new LineSpacingDependencyRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        Assert.Contains(LineSpacingDependencyRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenLineSpacingDependencyRuleIsHidden_ExcludesRule()
    {
        var result = InvokeGetAvailableRules(new LineSpacingDependencyRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        Assert.DoesNotContain(LineSpacingDependencyRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(LineSpacingDependencyRule.RuleId);
        var service = CreateService(
            [rule],
            new LineSpacingDependencyRuleOptions
            {
                Availability = RuleAvailability.Hidden
            });

        using var stream = CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [LineSpacingDependencyRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateLineSpacingDependencyRule(new LineSpacingDependencyRuleOptions
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
        var result = ValidateLineSpacingDependencyRule(new LineSpacingDependencyRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsLineSpacingDependencyWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(LineSpacingDependencyRule.RuleId);
        var unselectedRule = new RecordingRule("Grammar");
        var service = CreateService(
            [selectedRule, unselectedRule],
            new LineSpacingDependencyRuleOptions
            {
                Availability = RuleAvailability.Available
            });

        using var stream = CreateDocxStream();
        service.Validate(stream, new UniversityConfig(), [LineSpacingDependencyRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    [Fact]
    public void Validate_WhenTargetLineSpacingIsConfigured_UsesConfiguredLineSpacing()
    {
        var options = new LineSpacingDependencyRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error,
            TargetLineSpacingTwips = 240
        };
        var rule = CreateRule(options);
        using var docx = CreateDocxWithLineSpacing(240, LineSpacingRuleValues.Auto, null, 120);

        var results = rule.Validate(docx, new UniversityConfig(), null).ToList();

        Assert.Empty(results);
    }

    private static ValidationResult ValidateLineSpacingDependencyRule(LineSpacingDependencyRuleOptions options)
    {
        var rule = CreateRule(options);
        using var docx = CreateDocxWithLineSpacing(240, LineSpacingRuleValues.Auto, null, 120);

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static LineSpacingDependencyRule CreateRule(LineSpacingDependencyRuleOptions options)
    {
        return new LineSpacingDependencyRule(
            ruleConfigurationService: CreateRuleConfigurationService(options),
            options: Options.Create(options));
    }

    private static IResult InvokeGetAvailableRules(LineSpacingDependencyRuleOptions options)
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
                    [new RecordingRule(LineSpacingDependencyRule.RuleId), new RecordingRule("Grammar")],
                    options),
                CreateRuleConfigurationService(options)
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static ThesisValidatorService CreateService(
        IEnumerable<IValidationRule> rules,
        LineSpacingDependencyRuleOptions options)
    {
        return new ThesisValidatorService(rules, CreateRuleConfigurationService(options));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        LineSpacingDependencyRuleOptions options)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            lineSpacingDependencyOptions: Options.Create(options));
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

    private static WordprocessingDocument CreateDocxWithLineSpacing(
        int lineValue,
        LineSpacingRuleValues? lineRule,
        int? beforeTwips,
        int? afterTwips)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var spacing = new SpacingBetweenLines { Line = lineValue.ToString() };

        if (lineRule.HasValue)
            spacing.LineRule = lineRule.Value;

        if (beforeTwips.HasValue)
            spacing.Before = beforeTwips.Value.ToString();

        if (afterTwips.HasValue)
            spacing.After = afterTwips.Value.ToString();

        mainPart.Document.Body!.Append(
            new Paragraph(
                new ParagraphProperties(spacing),
                new Run(new Text("Test paragraph"))));
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
