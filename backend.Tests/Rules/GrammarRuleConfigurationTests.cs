using System.Net;
using System.Text.Json;
using backend.Models;
using backend.Rules;
using backend.RuleOptions;
using backend.Services.Analysis;
using backend.Services.CodeBlocks;
using backend.Services.Language;
using backend.Services.Rules;
using backend.Tests.Helpers;
using Backend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class GrammarRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenGrammarRuleIsAvailable_IncludesRule()
    {
        var service = CreateRuleConfigurationService(new GrammarRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        var result = RuleConfigurationTestSupport.InvokeGetAvailableRules(
            [new RecordingRule(GrammarRule.RuleId), new RecordingRule(FigureCaptionFormatRule.RuleId)],
            service);

        Assert.Contains(GrammarRule.RuleId, RuleConfigurationTestSupport.GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenGrammarRuleIsHidden_ExcludesRule()
    {
        var service = CreateRuleConfigurationService(new GrammarRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        var result = RuleConfigurationTestSupport.InvokeGetAvailableRules(
            [new RecordingRule(GrammarRule.RuleId), new RecordingRule(FigureCaptionFormatRule.RuleId)],
            service);

        Assert.DoesNotContain(GrammarRule.RuleId, RuleConfigurationTestSupport.GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(GrammarRule.RuleId);
        var service = new ThesisValidatorService(
            [rule],
            CreateRuleConfigurationService(new GrammarRuleOptions
            {
                Availability = RuleAvailability.Hidden
            }));

        using var stream = RuleConfigurationTestSupport.CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [GrammarRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateGrammarRule(new GrammarRuleOptions
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
        var result = ValidateGrammarRule(new GrammarRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsGrammarWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(GrammarRule.RuleId);
        var unselectedRule = new RecordingRule(FigureCaptionFormatRule.RuleId);
        var service = new ThesisValidatorService(
            [selectedRule, unselectedRule],
            CreateRuleConfigurationService(new GrammarRuleOptions
            {
                Availability = RuleAvailability.Available
            }));

        using var stream = RuleConfigurationTestSupport.CreateDocxStream();
        service.Validate(stream, new UniversityConfig(), [GrammarRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    private static ValidationResult ValidateGrammarRule(GrammarRuleOptions options)
    {
        var rule = new GrammarRule(
            CreateMockLanguageToolService(CreateGrammarResponse()),
            CodeBlockDetector.CreateDefault(),
            CreateRuleConfigurationService(options),
            Options.Create(options));
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("This speling is wrong.", "Times New Roman"));

        return Assert.Single(rule.Validate(docx.Document, new UniversityConfig { Language = "en-US" }));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        GrammarRuleOptions options)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            grammarOptions: Options.Create(options));
    }

    private static LanguageToolResponse CreateGrammarResponse()
    {
        return new LanguageToolResponse
        {
            Matches =
            [
                new LanguageToolMatch
                {
                    Message = "Possible spelling mistake found",
                    Offset = 5,
                    Length = 7,
                    Sentence = "This speling is wrong.",
                    Replacements =
                    [
                        new LanguageToolReplacement { Value = "spelling" }
                    ],
                    Rule = new LanguageToolRule
                    {
                        Id = "MORFOLOGIK_RULE_EN_US",
                        IssueType = "misspelling",
                        Category = new LanguageToolCategory { Id = "TYPOS", Name = "Possible Typo" }
                    }
                }
            ]
        };
    }

    private static LanguageToolService CreateMockLanguageToolService(LanguageToolResponse response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/v2/languages")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/v2/check")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LanguageTool:BaseUrl"] = "http://localhost:8010"
            })
            .Build();

        return new LanguageToolService(new HttpClient(handlerMock.Object), configuration);
    }
}
