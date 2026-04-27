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
using Rules;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class LegacyFormattingRuleConfigurationTests
{
    public static IEnumerable<object[]> RuleIds =>
    [
        [NoDotsInTitlesRule.RuleId],
        [ParagraphIndentRule.RuleId],
        [SingleSpaceRule.RuleId],
        [TextJustificationRule.RuleId],
        [TocRule.RuleId],
        [ManualTableOfContentsRule.RuleId]
    ];

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GetAvailableRules_WhenRuleIsAvailable_IncludesRule(string ruleId)
    {
        var result = InvokeGetAvailableRules(
            ruleId,
            CreateRuleConfigurationService(ruleId, RuleAvailability.Available));

        Assert.Contains(ruleId, GetRuleNames(result));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GetAvailableRules_WhenRuleIsHidden_ExcludesRule(string ruleId)
    {
        var result = InvokeGetAvailableRules(
            ruleId,
            CreateRuleConfigurationService(ruleId, RuleAvailability.Hidden));

        Assert.DoesNotContain(ruleId, GetRuleNames(result));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule(string ruleId)
    {
        var rule = new RecordingRule(ruleId);
        var service = new ThesisValidatorService(
            [rule],
            CreateRuleConfigurationService(ruleId, RuleAvailability.Hidden));

        using var stream = CreateDocxStream(new Paragraph(new Run(new Text("Body text"))));
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [ruleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void Validate_WhenSeverityIsWarning_AppliesWarning(string ruleId)
    {
        var result = ValidateConfiguredRule(ruleId, RuleSeverity.Warning);

        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.False(result.IsError);
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void Validate_WhenSeverityIsError_AppliesError(string ruleId)
    {
        var result = ValidateConfiguredRule(ruleId, RuleSeverity.Error);

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void Validate_WithSelectedRules_RunsSelectedRuleWithoutRunningUnselectedRule(string ruleId)
    {
        var selectedRule = new RecordingRule(ruleId);
        var unselectedRule = new RecordingRule("Grammar");
        var service = new ThesisValidatorService(
            [selectedRule, unselectedRule],
            CreateRuleConfigurationService(ruleId, RuleAvailability.Available));

        using var stream = CreateDocxStream(new Paragraph(new Run(new Text("Body text"))));
        service.Validate(stream, new UniversityConfig(), [ruleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    [Fact]
    public void Validate_WhenNoDotsTargetStylePatternIsConfigured_UsesConfiguredPattern()
    {
        var options = new NoDotsInTitlesRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error,
            TargetStylePatterns = ["customsection"]
        };
        var rule = new NoDotsInTitlesRule(
            CreateRuleConfigurationService(noDotsOptions: options),
            Options.Create(options));
        using var docx = CreateDocx(
            new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "CustomSection" }),
                new Run(new Text("Configured heading."))));

        var result = Assert.Single(rule.Validate(docx, new UniversityConfig(), null));

        Assert.Equal(NoDotsInTitlesRule.RuleId, result.RuleName);
    }

    [Fact]
    public void Validate_WhenParagraphIndentOptionsAreConfigured_UsesConfiguredAllowedIndentAndTolerance()
    {
        var options = new ParagraphIndentRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error,
            AllowedIndentTwips = [100],
            ToleranceTwips = 100
        };
        var rule = new ParagraphIndentRule(
            ruleConfigurationService: CreateRuleConfigurationService(paragraphIndentOptions: options),
            options: Options.Create(options));
        using var docx = CreateDocx(new Paragraph(new Run(new Text("Body text"))));

        var results = rule.Validate(docx, new UniversityConfig(), null).ToList();

        Assert.Empty(results);
    }

    private static ValidationResult ValidateConfiguredRule(string ruleId, RuleSeverity severity)
    {
        return ruleId switch
        {
            NoDotsInTitlesRule.RuleId => ValidateNoDotsInTitlesRule(severity),
            ParagraphIndentRule.RuleId => ValidateParagraphIndentRule(severity),
            SingleSpaceRule.RuleId => ValidateSingleSpaceRule(severity),
            TextJustificationRule.RuleId => ValidateTextJustificationRule(severity),
            TocRule.RuleId => ValidateTocRule(severity),
            ManualTableOfContentsRule.RuleId => ValidateManualTableOfContentsRule(severity),
            _ => throw new ArgumentOutOfRangeException(nameof(ruleId), ruleId, "Unknown rule id.")
        };
    }

    private static ValidationResult ValidateNoDotsInTitlesRule(RuleSeverity severity)
    {
        var options = new NoDotsInTitlesRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = severity
        };
        var rule = new NoDotsInTitlesRule(
            CreateRuleConfigurationService(noDotsOptions: options),
            Options.Create(options));
        using var docx = CreateDocx(
            new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
                new Run(new Text("Heading."))));

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static ValidationResult ValidateParagraphIndentRule(RuleSeverity severity)
    {
        var options = new ParagraphIndentRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = severity
        };
        var rule = new ParagraphIndentRule(
            ruleConfigurationService: CreateRuleConfigurationService(paragraphIndentOptions: options),
            options: Options.Create(options));
        using var docx = CreateDocx(new Paragraph(new Run(new Text("Body text"))));

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static ValidationResult ValidateSingleSpaceRule(RuleSeverity severity)
    {
        var options = new SingleSpaceRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = severity
        };
        var rule = new SingleSpaceRule(
            ruleConfigurationService: CreateRuleConfigurationService(singleSpaceOptions: options),
            options: Options.Create(options));
        using var docx = CreateDocx(
            new Paragraph(
                new Run(new Text("Two  spaces") { Space = SpaceProcessingModeValues.Preserve })));

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static ValidationResult ValidateTextJustificationRule(RuleSeverity severity)
    {
        var options = new TextJustificationRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = severity
        };
        var rule = new TextJustificationRule(
            ruleConfigurationService: CreateRuleConfigurationService(textJustificationOptions: options),
            options: Options.Create(options));
        using var docx = CreateDocx(
            new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                new Run(new Text("Body text"))));

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static ValidationResult ValidateTocRule(RuleSeverity severity)
    {
        var options = new TocRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = severity
        };
        var rule = new TocRule(
            CreateRuleConfigurationService(tocOptions: options),
            Options.Create(options));
        using var docx = CreateDocx(new Paragraph(new Run(new Text("Chapter 1"))));

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static ValidationResult ValidateManualTableOfContentsRule(RuleSeverity severity)
    {
        var options = new ManualTableOfContentsRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = severity
        };
        var rule = new ManualTableOfContentsRule(
            CreateRuleConfigurationService(manualTableOfContentsOptions: options),
            Options.Create(options));
        using var docx = CreateDocx(new Paragraph(new Run(new Text("Spis tresci"))));

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

    private static IRuleConfigurationService CreateRuleConfigurationService(
        string ruleId,
        RuleAvailability availability,
        RuleSeverity severity = RuleSeverity.Error)
    {
        return ruleId switch
        {
            NoDotsInTitlesRule.RuleId => CreateRuleConfigurationService(
                noDotsOptions: new NoDotsInTitlesRuleOptions
                {
                    Availability = availability,
                    Severity = severity
                }),
            ParagraphIndentRule.RuleId => CreateRuleConfigurationService(
                paragraphIndentOptions: new ParagraphIndentRuleOptions
                {
                    Availability = availability,
                    Severity = severity
                }),
            SingleSpaceRule.RuleId => CreateRuleConfigurationService(
                singleSpaceOptions: new SingleSpaceRuleOptions
                {
                    Availability = availability,
                    Severity = severity
                }),
            TextJustificationRule.RuleId => CreateRuleConfigurationService(
                textJustificationOptions: new TextJustificationRuleOptions
                {
                    Availability = availability,
                    Severity = severity
                }),
            TocRule.RuleId => CreateRuleConfigurationService(
                tocOptions: new TocRuleOptions
                {
                    Availability = availability,
                    Severity = severity
                }),
            ManualTableOfContentsRule.RuleId => CreateRuleConfigurationService(
                manualTableOfContentsOptions: new ManualTableOfContentsRuleOptions
                {
                    Availability = availability,
                    Severity = severity
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(ruleId), ruleId, "Unknown rule id.")
        };
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        NoDotsInTitlesRuleOptions? noDotsOptions = null,
        ParagraphIndentRuleOptions? paragraphIndentOptions = null,
        SingleSpaceRuleOptions? singleSpaceOptions = null,
        TextJustificationRuleOptions? textJustificationOptions = null,
        TocRuleOptions? tocOptions = null,
        ManualTableOfContentsRuleOptions? manualTableOfContentsOptions = null)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            noDotsInTitlesOptions: Options.Create(noDotsOptions ?? new NoDotsInTitlesRuleOptions()),
            paragraphIndentOptions: Options.Create(paragraphIndentOptions ?? new ParagraphIndentRuleOptions()),
            singleSpaceOptions: Options.Create(singleSpaceOptions ?? new SingleSpaceRuleOptions()),
            textJustificationOptions: Options.Create(textJustificationOptions ?? new TextJustificationRuleOptions()),
            tocOptions: Options.Create(tocOptions ?? new TocRuleOptions()),
            manualTableOfContentsOptions: Options.Create(manualTableOfContentsOptions ?? new ManualTableOfContentsRuleOptions()));
    }

    private static MemoryStream CreateDocxStream(params OpenXmlElement[] bodyChildren)
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            foreach (var child in bodyChildren)
            {
                mainPart.Document.Body!.Append(child.CloneNode(true));
            }

            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static WordprocessingDocument CreateDocx(params OpenXmlElement[] bodyChildren)
    {
        var stream = CreateDocxStream(bodyChildren);
        return WordprocessingDocument.Open(stream, true);
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
