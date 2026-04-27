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

public class ListSplitRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenListPunctuationConsistencyRuleIsAvailable_IncludesRule()
    {
        var result = InvokeGetAvailableRules(
            ListPunctuationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                punctuationOptions: new ListPunctuationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Available
                }));

        Assert.Contains(ListPunctuationConsistencyRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenListPunctuationConsistencyRuleIsHidden_ExcludesRule()
    {
        var result = InvokeGetAvailableRules(
            ListPunctuationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                punctuationOptions: new ListPunctuationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Hidden
                }));

        Assert.DoesNotContain(ListPunctuationConsistencyRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenListPunctuationConsistencyRuleIsManuallySelected_DoesNotExecuteRule()
    {
        AssertHiddenRuleIsNotExecuted(
            ListPunctuationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                punctuationOptions: new ListPunctuationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Hidden
                }));
    }

    [Fact]
    public void Validate_WhenListPunctuationConsistencySeverityIsWarning_AppliesWarning()
    {
        var result = ValidateListPunctuationConsistencyRule(new ListPunctuationConsistencyRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Warning
        });

        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.False(result.IsError);
    }

    [Fact]
    public void Validate_WhenListPunctuationConsistencySeverityIsError_AppliesError()
    {
        var result = ValidateListPunctuationConsistencyRule(new ListPunctuationConsistencyRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsListPunctuationConsistencyWithoutRunningUnselectedRule()
    {
        AssertSelectedRuleBehavior(
            ListPunctuationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                punctuationOptions: new ListPunctuationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Available
                }));
    }

    [Fact]
    public void GetAvailableRules_WhenListIndentationConsistencyRuleIsAvailable_IncludesRule()
    {
        var result = InvokeGetAvailableRules(
            ListIndentationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                indentationOptions: new ListIndentationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Available
                }));

        Assert.Contains(ListIndentationConsistencyRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenListIndentationConsistencyRuleIsHidden_ExcludesRule()
    {
        var result = InvokeGetAvailableRules(
            ListIndentationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                indentationOptions: new ListIndentationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Hidden
                }));

        Assert.DoesNotContain(ListIndentationConsistencyRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenListIndentationConsistencyRuleIsManuallySelected_DoesNotExecuteRule()
    {
        AssertHiddenRuleIsNotExecuted(
            ListIndentationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                indentationOptions: new ListIndentationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Hidden
                }));
    }

    [Fact]
    public void Validate_WhenListIndentationConsistencySeverityIsWarning_AppliesWarning()
    {
        var result = ValidateListIndentationConsistencyRule(new ListIndentationConsistencyRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Warning
        });

        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.False(result.IsError);
    }

    [Fact]
    public void Validate_WhenListIndentationConsistencySeverityIsError_AppliesError()
    {
        var result = ValidateListIndentationConsistencyRule(new ListIndentationConsistencyRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsListIndentationConsistencyWithoutRunningUnselectedRule()
    {
        AssertSelectedRuleBehavior(
            ListIndentationConsistencyRule.RuleId,
            CreateRuleConfigurationService(
                indentationOptions: new ListIndentationConsistencyRuleOptions
                {
                    Availability = RuleAvailability.Available
                }));
    }

    private static ValidationResult ValidateListPunctuationConsistencyRule(
        ListPunctuationConsistencyRuleOptions options)
    {
        var ruleConfigurationService = CreateRuleConfigurationService(punctuationOptions: options);
        var rule = new ListPunctuationConsistencyRule(
            ruleConfigurationService,
            Options.Create(options));
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1),
            CreateListItem("Second item,", 1),
            CreateListItem("Third item.", 1));

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static ValidationResult ValidateListIndentationConsistencyRule(
        ListIndentationConsistencyRuleOptions options)
    {
        var ruleConfigurationService = CreateRuleConfigurationService(indentationOptions: options);
        var rule = new ListIndentationConsistencyRule(
            ruleConfigurationService,
            Options.Create(options));
        using var docx = CreateDocxWithParagraphs(
            CreateListItem("First item;", 1, indentTwips: 720),
            CreateListItem("Second item;", 1, indentTwips: 1440),
            CreateListItem("Third item.", 1, indentTwips: 720));

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static IResult InvokeGetAvailableRules(
        string ruleId,
        IRuleConfigurationService ruleConfigurationService)
    {
        var method = typeof(DocumentEndpoint).GetMethod(
            "GetAvailableRules",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(
            null,
            new object?[]
            {
                new ThesisValidatorService(
                    [new RecordingRule(ruleId), new RecordingRule("Grammar")],
                    ruleConfigurationService),
                ruleConfigurationService
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static void AssertHiddenRuleIsNotExecuted(
        string ruleId,
        IRuleConfigurationService ruleConfigurationService)
    {
        var rule = new RecordingRule(ruleId);
        var service = new ThesisValidatorService([rule], ruleConfigurationService);

        using var stream = CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [ruleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    private static void AssertSelectedRuleBehavior(
        string ruleId,
        IRuleConfigurationService ruleConfigurationService)
    {
        var selectedRule = new RecordingRule(ruleId);
        var unselectedRule = new RecordingRule("Grammar");
        var service = new ThesisValidatorService(
            [selectedRule, unselectedRule],
            ruleConfigurationService);

        using var stream = CreateDocxStream();
        service.Validate(stream, new UniversityConfig(), [ruleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        ListPunctuationConsistencyRuleOptions? punctuationOptions = null,
        ListIndentationConsistencyRuleOptions? indentationOptions = null)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            listPunctuationConsistencyOptions: Options.Create(punctuationOptions ?? new ListPunctuationConsistencyRuleOptions()),
            listIndentationConsistencyOptions: Options.Create(indentationOptions ?? new ListIndentationConsistencyRuleOptions()));
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

    private static WordprocessingDocument CreateDocxWithParagraphs(params Paragraph[] paragraphs)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        foreach (var paragraph in paragraphs)
        {
            mainPart.Document.Body!.Append(paragraph);
        }

        mainPart.Document.Save();
        stream.Position = 0;
        return doc;
    }

    private static Paragraph CreateListItem(
        string text,
        int numberingId,
        int level = 0,
        int? indentTwips = null)
    {
        var numberingProps = new NumberingProperties(
            new NumberingLevelReference { Val = level },
            new NumberingId { Val = numberingId }
        );

        var paraProps = new ParagraphProperties(numberingProps);
        if (indentTwips.HasValue)
        {
            paraProps.Indentation = new Indentation { Left = indentTwips.Value.ToString() };
        }

        return new Paragraph(
            paraProps,
            new Run(new Text(text))
        );
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
