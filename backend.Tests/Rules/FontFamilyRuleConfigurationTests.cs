using System.Collections;
using System.Reflection;
using backend.Endpoints;
using backend.Models;
using backend.Rules;
using backend.RuleOptions;
using backend.Services.Analysis;
using backend.Services.Comments;
using backend.Services.Rules;
using backend.Tests.Helpers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class FontFamilyRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenFontFamilyRuleIsAvailable_IncludesRule()
    {
        var result = InvokeGetAvailableRules(new FontFamilyRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        Assert.Contains(FontFamilyValidationRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenFontFamilyRuleIsHidden_ExcludesRule()
    {
        var result = InvokeGetAvailableRules(new FontFamilyRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        Assert.DoesNotContain(FontFamilyValidationRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(FontFamilyValidationRule.RuleId);
        var service = CreateService(
            [rule],
            new FontFamilyRuleOptions
            {
                Availability = RuleAvailability.Hidden
            });

        using var stream = CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            CreateConfig(),
            [FontFamilyValidationRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateFontFamilyRule(new FontFamilyRuleOptions
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
        var result = ValidateFontFamilyRule(new FontFamilyRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WhenRequiredFontFamilyIsConfigured_UsesConfiguredFont()
    {
        var rule = new FontFamilyValidationRule(
            ruleConfigurationService: CreateRuleConfigurationService(new FontFamilyRuleOptions()),
            options: Options.Create(new FontFamilyRuleOptions
            {
                Availability = RuleAvailability.Available,
                Severity = RuleSeverity.Error,
                RequiredFontFamily = "Arial"
            }));
        using var docx = DocxTestHelper.CreateInMemoryDocx(("Configured font paragraph", "Arial"));

        var results = rule.Validate(docx.Document, CreateConfig()).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsFontFamilyWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(FontFamilyValidationRule.RuleId);
        var unselectedRule = new RecordingRule("Grammar");
        var service = CreateService(
            [selectedRule, unselectedRule],
            new FontFamilyRuleOptions
            {
                Availability = RuleAvailability.Available,
                Severity = RuleSeverity.Error
            });

        using var stream = CreateDocxStream();
        service.Validate(stream, CreateConfig(), [FontFamilyValidationRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    private static ValidationResult ValidateFontFamilyRule(FontFamilyRuleOptions options)
    {
        var rule = new FontFamilyValidationRule(
            ruleConfigurationService: CreateRuleConfigurationService(options),
            options: Options.Create(options));
        using var docx = DocxTestHelper.CreateInMemoryDocx(("Wrong font paragraph", "Arial"));

        return Assert.Single(rule.Validate(docx.Document, CreateConfig()));
    }

    private static IResult InvokeGetAvailableRules(FontFamilyRuleOptions options)
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
                    [new RecordingRule(FontFamilyValidationRule.RuleId), new RecordingRule("Grammar")],
                    options),
                CreateRuleConfigurationService(options)
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static ThesisValidatorService CreateService(
        IEnumerable<IValidationRule> rules,
        FontFamilyRuleOptions options)
    {
        return new ThesisValidatorService(rules, CreateRuleConfigurationService(options));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(FontFamilyRuleOptions options)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            Options.Create(options));
    }

    private static UniversityConfig CreateConfig()
    {
        return new UniversityConfig
        {
            Formatting = new FormattingConfig
            {
                Font = new FontConfig { FontFamily = "Times New Roman" }
            }
        };
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
