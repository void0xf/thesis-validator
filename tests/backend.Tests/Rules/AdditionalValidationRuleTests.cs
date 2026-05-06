using System.Net;
using backend.Infrastructure.LanguageTool;
using backend.Rules;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public sealed class NoDotsInTitlesRuleTests
{
    [Fact]
    public void Validate_WhenHeadingEndsWithPeriod_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("Chapter title.", styleId: "Heading1"));

        var problem = Assert.Single(new NoDotsInTitlesRule()
            .Validate(document.Context, new NoDotsInTitlesRuleOptions()));

        Assert.Contains("should not end with a period", problem.Message);
    }
}

public sealed class SingleSpaceRuleTests
{
    [Fact]
    public void Validate_WhenParagraphContainsMultipleSpaces_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("word  word"));

        var problem = Assert.Single(new SingleSpaceRule()
            .Validate(document.Context, new SingleSpaceRuleOptions()));

        Assert.Equal(4, problem.Location.CharacterOffset);
        Assert.Equal(2, problem.Location.Length);
    }
}

public sealed class TextJustificationRuleTests
{
    [Fact]
    public void Validate_WhenBodyParagraphIsCentered_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph(
                "Centered body paragraph",
                paragraphProperties: new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center })));

        var problem = Assert.Single(new TextJustificationRule()
            .Validate(document.Context, new TextJustificationRuleOptions()));

        Assert.Contains("center aligned", problem.Message);
    }
}

public sealed class GrammarRuleTests
{
    [Fact]
    public void Validate_WhenLanguageToolIsUnavailable_ReturnsSkippedProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("Tekst do sprawdzenia"));
        var rule = new GrammarRule(CreateLanguageToolClient(HttpStatusCode.ServiceUnavailable));

        var problem = Assert.Single(rule.Validate(document.Context, new GrammarRuleOptions()));

        Assert.Contains("LanguageTool service is not available", problem.Message);
    }

    private static LanguageToolClient CreateLanguageToolClient(HttpStatusCode statusCode)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LanguageTool:BaseUrl"] = "http://languagetool.test"
            })
            .Build();
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode));

        return new LanguageToolClient(httpClient, configuration);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public StubHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}

public sealed class LineSpacingDependencyRuleTests
{
    [Fact]
    public void Validate_WhenLineSpacingIsNotRequiredValue_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph(
                "Body paragraph",
                paragraphProperties: new ParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Line = "240",
                        LineRule = LineSpacingRuleValues.Auto
                    })));

        var problem = Assert.Single(new LineSpacingDependencyRule()
            .Validate(document.Context, new LineSpacingDependencyRuleOptions()));

        Assert.Contains("line spacing must be", problem.Message);
    }
}

public sealed class ListIndentationConsistencyRuleTests
{
    [Fact]
    public void Validate_WhenSameLevelListItemUsesDifferentIndent_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.ListParagraph("First item", leftIndent: 720),
            RuleTestDocument.ListParagraph("Second item", leftIndent: 720),
            RuleTestDocument.ListParagraph("Indented item", leftIndent: 1440));

        var problem = Assert.Single(new ListIndentationConsistencyRule()
            .Validate(document.Context, new ListIndentationConsistencyRuleOptions()));

        Assert.Contains("inconsistent indentation", problem.Message);
    }
}

public sealed class ListPunctuationConsistencyRuleTests
{
    [Fact]
    public void Validate_WhenMiddleItemUsesDifferentPunctuation_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.ListParagraph("First item;", leftIndent: 720),
            RuleTestDocument.ListParagraph("Second item.", leftIndent: 720),
            RuleTestDocument.ListParagraph("Third item.", leftIndent: 720));

        var problem = Assert.Single(new ListPunctuationConsistencyRule()
            .Validate(document.Context, new ListPunctuationConsistencyRuleOptions()));

        Assert.Contains("first item uses ';'", problem.Message);
    }
}

public sealed class ParagraphIndentRuleTests
{
    [Fact]
    public void Validate_WhenFirstLineIndentIsMissing_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("Indented paragraph required"));

        var problem = Assert.Single(new ParagraphIndentRule()
            .Validate(document.Context, new ParagraphIndentRuleOptions()));

        Assert.Contains("incorrect first line indent", problem.Message);
    }
}

public sealed class ParagraphSpacingRuleTests
{
    [Fact]
    public void Validate_WhenSpacingAfterIsNotAllowed_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph(
                "Paragraph with wrong spacing",
                paragraphProperties: new ParagraphProperties(
                    new SpacingBetweenLines { After = "240" })));

        var problem = Assert.Single(new ParagraphSpacingRule()
            .Validate(document.Context, new ParagraphSpacingRuleOptions()));

        Assert.Contains("incorrect spacing", problem.Message);
    }
}

public sealed class FigureCaptionFormatRuleTests
{
    [Fact]
    public void Validate_WhenFigureCaptionHasInvalidLabel_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.FigureParagraph(),
            RuleTestDocument.Paragraph("Obrazek: 1 Invalid caption"));

        var problem = Assert.Single(new FigureCaptionFormatRule()
            .Validate(document.Context, new FigureCaptionFormatRuleOptions()));

        Assert.Contains("invalid format", problem.Message);
    }
}

public sealed class FigureCaptionPositionRuleTests
{
    [Fact]
    public void Validate_WhenCaptionIsAboveFigure_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("Rys. 1 Caption", styleId: "Caption"),
            RuleTestDocument.FigureParagraph());

        var problem = Assert.Single(new FigureCaptionPositionRule()
            .Validate(document.Context, new FigureCaptionPositionRuleOptions()));

        Assert.Contains("below the figure", problem.Message);
    }
}

public sealed class HeadingStyleUsageRuleTests
{
    [Fact]
    public void Validate_WhenParagraphLooksLikeManualHeading_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph(
                "Manual heading",
                runProperties: new RunProperties(
                    new Bold(),
                    new FontSize { Val = "32" })));

        var problem = Assert.Single(new HeadingStyleUsageRule()
            .Validate(document.Context, new HeadingStyleUsageRuleOptions()));

        Assert.Contains("manually formatted as a heading", problem.Message);
    }
}

public sealed class HierarchyDepthRuleTests
{
    [Fact]
    public void Validate_WhenHeadingLevelIsTooDeep_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("Too deep heading", styleId: "Heading4"));

        var problem = Assert.Single(new HierarchyDepthRule()
            .Validate(document.Context, new HierarchyDepthRuleOptions()));

        Assert.Contains("Structure too deep", problem.Message);
    }
}

public sealed class ManualTableOfContentsRuleTests
{
    [Fact]
    public void Validate_WhenManualTableOfContentsHeadingExists_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("Spis tresci"));

        var problem = Assert.Single(new ManualTableOfContentsRule()
            .Validate(document.Context, new ManualTableOfContentsRuleOptions()));

        Assert.Contains("probably created manually", problem.Message);
    }
}

public sealed class MissingFigureCaptionRuleTests
{
    [Fact]
    public void Validate_WhenFigureHasNoCaption_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.FigureParagraph());

        var problem = Assert.Single(new MissingFigureCaptionRule()
            .Validate(document.Context, new MissingFigureCaptionRuleOptions()));

        Assert.Contains("Figure has no caption", problem.Message);
    }
}

public sealed class TocRuleTests
{
    [Fact]
    public void Validate_WhenAutomaticTableOfContentsIsMissing_ReturnsProblem()
    {
        using var document = RuleTestDocument.Create(
            RuleTestDocument.Paragraph("Introduction"));

        var problem = Assert.Single(new TocRule()
            .Validate(document.Context, new TocRuleOptions()));

        Assert.Contains("missing an automatic Word Table of Contents", problem.Message);
    }
}
