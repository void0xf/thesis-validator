using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using backend.Annotation;
using backend.Application.Validation;
using backend.DocumentProcessing.CodeBlocks;
using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Documents;
using backend.Rules;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Application.Validation;

public sealed class CodeBlockDetectionIntegrationTests
{
    [Fact]
    public void Analyze_WhenParagraphMeetsCodeFontThreshold_ExcludesParagraphFromDocumentContent()
    {
        using var stream = CreateDocxStream(
            ("Console.WriteLine(\"bad\");", "Consolas"),
            ("Regular body text", "Times New Roman"));
        using var document = WordprocessingDocument.Open(stream, false);
        var analyzer = new DocumentContentAnalyzer(
            new DocumentSkipResolver(Options.Create(new ValidationSkippingOptions())),
            new CodeBlockDetector(Options.Create(new CodeBlockDetectionOptions
            {
                MinimumCodeFontTextRatio = 0.7
            })));

        var content = analyzer.Analyze(document);

        var paragraph = Assert.Single(content.BodyChildParagraphs);
        Assert.Equal("Regular body text", paragraph.Text);
        Assert.Equal(2, paragraph.BodyIndex);
    }

    [Fact]
    public void Validate_WhenParagraphMeetsCodeFontThreshold_SkipsParagraphLevelRule()
    {
        var service = CreateService(new CodeBlockDetectionOptions
        {
            MinimumCodeFontTextRatio = 0.7
        });
        using var stream = CreateDocxStream(("Console.WriteLine(\"bad\");", "Consolas"));

        var results = service.Validate(stream, [FontFamilyRule.RuleId]);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WhenParagraphIsBelowCodeFontThreshold_RunsParagraphLevelRule()
    {
        var service = CreateService(new CodeBlockDetectionOptions
        {
            MinimumCodeFontTextRatio = 0.7
        });
        using var stream = CreateDocxStreamWithRuns(
            ("code", "Consolas"),
            (" body text that dominates", "Arial"));

        var results = service.Validate(stream, [FontFamilyRule.RuleId]);

        Assert.NotEmpty(results);
    }

    private static ThesisValidationOrchestrator CreateService(
        CodeBlockDetectionOptions codeBlockOptions)
    {
        var policyResolver = new RulePolicyResolver(
            new ConfigurationBuilder().Build());
        var optionsBinder = new RuleOptionsBinder(
            new ConfigurationBuilder().Build());
        var resultComposer = new ValidationIssueComposer();
        var codeBlockDetector = new CodeBlockDetector(
            Options.Create(codeBlockOptions));

        return new ThesisValidationOrchestrator(
            new DocumentSession(),
            new DocumentContentAnalyzer(
                new DocumentSkipResolver(Options.Create(new ValidationSkippingOptions())),
                codeBlockDetector),
            new RuleRunner(
                [new FontFamilyRule()],
                policyResolver,
                optionsBinder,
                resultComposer),
            new SectionContextResolver(),
            new AnnotationApplicator());
    }

    private static MemoryStream CreateDocxStream(
        params (string Text, string? FontFamily)[] paragraphs)
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            foreach (var (text, fontFamily) in paragraphs)
            {
                var run = CreateRun(text, fontFamily);
                mainPart.Document.Body!.Append(new Paragraph(run));
            }

            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocxStreamWithRuns(
        params (string Text, string? FontFamily)[] runs)
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var paragraph = new Paragraph();
            foreach (var (text, fontFamily) in runs)
            {
                paragraph.Append(CreateRun(text, fontFamily));
            }

            mainPart.Document.Body!.Append(paragraph);
            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static Run CreateRun(string text, string? fontFamily)
    {
        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            run.RunProperties = new RunProperties(
                new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily });
        }

        return run;
    }
}
