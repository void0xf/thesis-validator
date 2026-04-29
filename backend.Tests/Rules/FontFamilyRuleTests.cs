using backend.Models;
using backend.ModernServices;
using backend.RuleOptions;
using backend.Rules;
using backend.Tests.Helpers;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Rules;

public class FontFamilyRuleTests
{
    private readonly FontFamilyRule _rule = new();

    [Fact]
    public void Validate_AllParagraphsWithCorrectFont_ReturnsNoProblems()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("First paragraph", "Times New Roman"),
            ("Second paragraph", "Times New Roman"));

        var problems = Validate(docx, new FontFamilyRuleOptions()).ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ParagraphWithWrongFont_ReturnsProblem()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Correct font paragraph", "Times New Roman"),
            ("Wrong font paragraph", "Arial"));

        var problems = Validate(docx, new FontFamilyRuleOptions()).ToList();

        var problem = Assert.Single(problems);
        Assert.Contains("Arial", problem.Message);
        Assert.Contains("Times New Roman", problem.Message);
    }

    [Fact]
    public void Validate_MultipleParagraphsWithWrongFonts_ReturnsMultipleProblems()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Arial paragraph", "Arial"),
            ("Calibri paragraph", "Calibri"),
            ("Correct paragraph", "Times New Roman"));

        var problems = Validate(docx, new FontFamilyRuleOptions()).ToList();

        Assert.Equal(2, problems.Count);
        Assert.Contains(problems, problem => problem.Message.Contains("Arial"));
        Assert.Contains(problems, problem => problem.Message.Contains("Calibri"));
    }

    [Fact]
    public void Validate_WithDefaultFontStyle_UsesDefaultFont()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocxWithDefaultFont(
            "Times New Roman",
            "Paragraph without explicit font");

        var problems = Validate(docx, new FontFamilyRuleOptions()).ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_WithConfiguredRequiredFont_UsesConfiguredFont()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Arial paragraph", "Arial"));

        var problems = Validate(docx, new FontFamilyRuleOptions
        {
            RequiredFontFamily = "Arial"
        }).ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_EmptyParagraph_IsSkipped()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("", "Arial"),
            ("   ", "Calibri"),
            ("Valid text", "Times New Roman"));

        var problems = Validate(docx, new FontFamilyRuleOptions()).ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ProblemContainsCorrectLocation()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("First paragraph", "Times New Roman"),
            ("Second paragraph with wrong font", "Arial"));

        var problems = Validate(docx, new FontFamilyRuleOptions()).ToList();

        var problem = Assert.Single(problems);
        Assert.Equal(2, problem.Location.Paragraph);
        Assert.Equal(1, problem.Location.Run);
        Assert.Equal(0, problem.Location.CharacterOffset);
        Assert.Equal("Second paragraph with wrong font".Length, problem.Location.Length);
        Assert.Equal("Second paragraph with wrong font", problem.Location.Text);
        Assert.Equal(ParagraphIndexKind.BodyElement, problem.ParagraphIndexKind);
        Assert.IsType<RunAnnotationTarget>(problem.AnnotationTarget);
    }

    [Fact]
    public void Validate_LocationTracksCharacterOffset()
    {
        using var docx = DocxTestHelper.CreateInMemoryDocxWithMultipleRuns(
            ("First run ", "Times New Roman"),
            ("Second run with wrong font", "Arial"));

        var problems = Validate(docx, new FontFamilyRuleOptions()).ToList();

        var problem = Assert.Single(problems);
        Assert.Equal(1, problem.Location.Paragraph);
        Assert.Equal(2, problem.Location.Run);
        Assert.Equal("First run ".Length, problem.Location.CharacterOffset);
    }

    [Fact]
    public void Validate_RuleDescriptorUsesFontFamilyRuleId()
    {
        Assert.Equal("FontFamily", _rule.Descriptor.Name);
    }

    private IEnumerable<RuleProblem> Validate(
        InMemoryDocx docx,
        FontFamilyRuleOptions options)
    {
        var analyzer = new DocumentContentAnalyzer(new ModernDocumentSkipService(
            Options.Create(new ModernValidationOptions())));
        var context = new RuleContext
        {
            RawDocument = docx.Document,
            Content = analyzer.Analyze(docx.Document)
        };

        return _rule.Validate(context, options);
    }
}
