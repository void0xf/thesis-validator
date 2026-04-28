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

public class FigureCaptionFormatRuleConfigurationTests
{
    [Fact]
    public void GetAvailableRules_WhenFigureCaptionFormatRuleIsAvailable_IncludesRule()
    {
        var service = CreateRuleConfigurationService(new FigureCaptionFormatRuleOptions
        {
            Availability = RuleAvailability.Available
        });

        var result = RuleConfigurationTestSupport.InvokeGetAvailableRules(
            [new RecordingRule(FigureCaptionFormatRule.RuleId), new RecordingRule(GrammarRule.RuleId)],
            service);

        Assert.Contains(FigureCaptionFormatRule.RuleId, RuleConfigurationTestSupport.GetRuleNames(result));
    }

    [Fact]
    public void GetAvailableRules_WhenFigureCaptionFormatRuleIsHidden_ExcludesRule()
    {
        var service = CreateRuleConfigurationService(new FigureCaptionFormatRuleOptions
        {
            Availability = RuleAvailability.Hidden
        });

        var result = RuleConfigurationTestSupport.InvokeGetAvailableRules(
            [new RecordingRule(FigureCaptionFormatRule.RuleId), new RecordingRule(GrammarRule.RuleId)],
            service);

        Assert.DoesNotContain(FigureCaptionFormatRule.RuleId, RuleConfigurationTestSupport.GetRuleNames(result));
    }

    [Fact]
    public void Validate_WhenHiddenRuleIsManuallySelected_DoesNotExecuteRule()
    {
        var rule = new RecordingRule(FigureCaptionFormatRule.RuleId);
        var service = new ThesisValidatorService(
            [rule],
            CreateRuleConfigurationService(new FigureCaptionFormatRuleOptions
            {
                Availability = RuleAvailability.Hidden
            }));

        using var stream = RuleConfigurationTestSupport.CreateDocxStream();
        var (results, _) = service.Validate(
            stream,
            new UniversityConfig(),
            [FigureCaptionFormatRule.RuleId]);

        Assert.Empty(results);
        Assert.Equal(0, rule.RunCount);
    }

    [Fact]
    public void Validate_WhenSeverityIsWarning_AppliesWarning()
    {
        var result = ValidateFigureCaptionFormatRule(new FigureCaptionFormatRuleOptions
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
        var result = ValidateFigureCaptionFormatRule(new FigureCaptionFormatRuleOptions
        {
            Availability = RuleAvailability.Available,
            Severity = RuleSeverity.Error
        });

        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Validate_WithSelectedRules_RunsFigureCaptionFormatWithoutRunningUnselectedRule()
    {
        var selectedRule = new RecordingRule(FigureCaptionFormatRule.RuleId);
        var unselectedRule = new RecordingRule(GrammarRule.RuleId);
        var service = new ThesisValidatorService(
            [selectedRule, unselectedRule],
            CreateRuleConfigurationService(new FigureCaptionFormatRuleOptions
            {
                Availability = RuleAvailability.Available
            }));

        using var stream = RuleConfigurationTestSupport.CreateDocxStream();
        service.Validate(stream, new UniversityConfig(), [FigureCaptionFormatRule.RuleId]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    private static ValidationResult ValidateFigureCaptionFormatRule(FigureCaptionFormatRuleOptions options)
    {
        var rule = new FigureCaptionFormatRule(
            CreateRuleConfigurationService(options),
            Options.Create(options));
        using var docx = CreateDocxWithFigureAndCaption("Obrazek 1: Invalid label");

        return Assert.Single(rule.Validate(docx, new UniversityConfig(), null));
    }

    private static IRuleConfigurationService CreateRuleConfigurationService(
        FigureCaptionFormatRuleOptions options)
    {
        return new RuleConfigurationService(
            Options.Create(new EmptySectionStructureRuleOptions()),
            figureCaptionFormatOptions: Options.Create(options));
    }

    private static WordprocessingDocument CreateDocxWithFigureAndCaption(string captionText)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            CreateFigureParagraph(),
            new Paragraph(new Run(new Text(captionText)))));
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
