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
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace backend.Tests.Rules;

public class MissingFigureCaptionRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenMissingFigureCaptionRuleIsAvailable_IncludesRule()
    {
        var result = InvokeGetAvailableRules(new MissingFigureCaptionRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        Assert.Contains(MissingFigureCaptionRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenMissingFigureCaptionRuleIsHidden_ExcludesRule()
    {
        var result = InvokeGetAvailableRules(new MissingFigureCaptionRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        Assert.DoesNotContain(MissingFigureCaptionRule.RuleId, GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(MissingFigureCaptionRule.RuleId);
        var service = CreateService(
            [rule],
            new MissingFigureCaptionRuleOptions
            {
                Availability = RuleAvailability.Hidden
            });

        using var stream = CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [MissingFigureCaptionRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateMissingFigureCaptionRule(new MissingFigureCaptionRuleOptions
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
        var result = ValidateMissingFigureCaptionRule(new MissingFigureCaptionRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsMissingFigureCaptionWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(MissingFigureCaptionRule.RuleId);
        var unselectedRule = new RecordingRule("Grammar");
        var service = CreateService(
            [selectedRule, unselectedRule],
            new MissingFigureCaptionRuleOptions
            {
                Availability = RuleAvailability.Available
            });

        using var stream = CreateDocxStream();
        service.Validate(stream, new UniversityConfig(), [MissingFigureCaptionRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    private static ValidationResult ValidateMissingFigureCaptionRule(MissingFigureCaptionRuleOptions options)
    {
        var rule = new MissingFigureCaptionRule(
            CreateRuleConfigurationService(options),
            Options.Create(options));
        using var docx = CreateDocxWithPicture();

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static IResult InvokeGetAvailableRules(MissingFigureCaptionRuleOptions options)
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
                    [new RecordingRule(MissingFigureCaptionRule.RuleId), new RecordingRule("Grammar")],
                    options),
                CreateRuleConfigurationService(options)
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static ThesisValidatorService CreateService(
        IEnumerable<IValidationRule> rules,
        MissingFigureCaptionRuleOptions options)
    {
        return new ThesisValidatorService(rules, CreateRuleConfigurationService(options));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        MissingFigureCaptionRuleOptions options)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            missingFigureCaptionOptions: Options.Create(options));
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

    private static WordprocessingDocument CreateDocxWithPicture()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            new Paragraph(
                new Run(
                    new Drawing(
                        new DW.Inline(
                            new A.Graphic(
                                new A.GraphicData(
                                    new PIC.Picture())
                                {
                                    Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                                })))))));
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
