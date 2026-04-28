using backend.Models;
using backend.Rules;
using backend.RuleOptions;
using backend.Services.Analysis;
using backend.Services.Rules;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace backend.Tests.Rules;

public class FigureCaptionPositionRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenFigureCaptionPositionRuleIsAvailable_IncludesRule()
    {
        var service = CreateRuleConfigurationService(new FigureCaptionPositionRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        var result = RuleConfigurationTestSupport.InvokeGetAvailableRules(
            [new RecordingRule(FigureCaptionPositionRule.RuleId), new RecordingRule(GrammarRule.RuleId)],
            service);

        Assert.Contains(FigureCaptionPositionRule.RuleId, RuleConfigurationTestSupport.GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenFigureCaptionPositionRuleIsHidden_ExcludesRule()
    {
        var service = CreateRuleConfigurationService(new FigureCaptionPositionRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        var result = RuleConfigurationTestSupport.InvokeGetAvailableRules(
            [new RecordingRule(FigureCaptionPositionRule.RuleId), new RecordingRule(GrammarRule.RuleId)],
            service);

        Assert.DoesNotContain(FigureCaptionPositionRule.RuleId, RuleConfigurationTestSupport.GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(FigureCaptionPositionRule.RuleId);
        var service = new ThesisValidatorService(
            [rule],
            CreateRuleConfigurationService(new FigureCaptionPositionRuleOptions
            {
                Availability = RuleAvailability.Hidden
            }));

        using var stream = RuleConfigurationTestSupport.CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [FigureCaptionPositionRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateFigureCaptionPositionRule(new FigureCaptionPositionRuleOptions
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
        var result = ValidateFigureCaptionPositionRule(new FigureCaptionPositionRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsFigureCaptionPositionWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(FigureCaptionPositionRule.RuleId);
        var unselectedRule = new RecordingRule(GrammarRule.RuleId);
        var service = new ThesisValidatorService(
            [selectedRule, unselectedRule],
            CreateRuleConfigurationService(new FigureCaptionPositionRuleOptions
            {
                Availability = RuleAvailability.Available
            }));

        using var stream = RuleConfigurationTestSupport.CreateDocxStream();
        service.Validate(stream, new UniversityConfig(), [FigureCaptionPositionRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    private static ValidationResult ValidateFigureCaptionPositionRule(FigureCaptionPositionRuleOptions options)
    {
        var rule = new FigureCaptionPositionRule(
            CreateRuleConfigurationService(options),
            Options.Create(options));
        using var docx = CreateDocxWithCaptionAboveFigure();

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        FigureCaptionPositionRuleOptions options)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            figureCaptionPositionOptions: Options.Create(options));
    }

    private static WordprocessingDocument CreateDocxWithCaptionAboveFigure()
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Caption" }),
                new Run(new Text("Rys. 1 Example caption"))),
            CreateFigureParagraph()));
        mainPart.Document.Save();
        stream.Position = 0;
        return doc;
    }

    private static Paragraph CreateFigureParagraph()
    {
        return new Paragraph(
            new Run(
                new Drawing(
                    new DW.Inline(
                        new A.Graphic(
                            new A.GraphicData(
                                new PIC.Picture())
                            {
                                Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                            })))));
    }
}
