using backend.Models;
using backend.Rules;
using backend.Tests.Helpers;
using Backend.Models;

namespace backend.Tests.Rules;

public class FontFamilyRuleTests
{
    private readonly FontFamilyValidationRule _rule = new();

    private static UniversityConfig CreateConfig(string fontFamily = "Times New Roman")
    {
        return new UniversityConfig
        {
            Formatting = new FormattingConfig
            {
                Font = new FontConfig { FontFamily = fontFamily }
            }
        };
    }

    [Fact]
    public void Validate_AllParagraphsWithCorrectFont_ReturnsNoErrors()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("First paragraph", "Times New Roman"),
            ("Second paragraph", "Times New Roman")
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ParagraphWithWrongFont_ReturnsError()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Correct font paragraph", "Times New Roman"),
            ("Wrong font paragraph", "Arial")
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Contains("Arial", errors[0].Message);
        Assert.Contains("Times New Roman", errors[0].Message);
        Assert.True(errors[0].IsError);
    }

    [Fact]
    public void Validate_MultipleParagraphsWithWrongFonts_ReturnsMultipleErrors()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Arial paragraph", "Arial"),
            ("Calibri paragraph", "Calibri"),
            ("Correct paragraph", "Times New Roman")
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Message.Contains("Arial"));
        Assert.Contains(errors, e => e.Message.Contains("Calibri"));
    }

    [Fact]
    public void Validate_WithDefaultFontStyle_UsesDefaultFont()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocxWithDefaultFont(
            "Times New Roman",
            "Paragraph without explicit font"
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ConfigWithDifferentExpectedFont_ValidatesAgainstConfigFont()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Arial paragraph", "Arial")
        );
        var config = CreateConfig("Arial"); // Expecting Arial

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Empty(errors); // Arial is now the expected font
    }

    [Fact]
    public void Validate_EmptyParagraph_IsSkipped()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("", "Arial"),
            ("   ", "Calibri"),
            ("Valid text", "Times New Roman")
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Empty(errors); // Empty/whitespace paragraphs should be skipped
    }

    [Fact]
    public void Validate_ErrorContainsCorrectLocation()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("First paragraph", "Times New Roman"),
            ("Second paragraph with wrong font", "Arial")
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Equal(2, errors[0].Location.Paragraph);
        Assert.Equal(1, errors[0].Location.Run);
        Assert.Equal(0, errors[0].Location.CharacterOffset);
        Assert.Equal("Second paragraph with wrong font".Length, errors[0].Location.Length);
        Assert.Equal("Second paragraph with wrong font", errors[0].Location.Text);
        Assert.True(errors[0].Location.PageNumber >= 1);
        Assert.True(errors[0].Location.LineNumber >= 1);
    }

    [Fact]
    public void Validate_LocationTracksCharacterOffset()
    {
        // Arrange - Create doc with multiple runs in same paragraph
        using var docx = DocxTestHelper.CreateInMemoryDocxWithMultipleRuns(
            ("First run ", "Times New Roman"),
            ("Second run with wrong font", "Arial")
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Equal(1, errors[0].Location.Paragraph);
        Assert.Equal(2, errors[0].Location.Run);
        Assert.Equal("First run ".Length, errors[0].Location.CharacterOffset);
    }

    [Fact]
    public void Validate_LocationIncludesPageAndLine()
    {
        // Arrange
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Wrong font paragraph", "Arial")
        );
        var config = CreateConfig("Times New Roman");

        // Act
        var errors = _rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Equal(1, errors[0].Location.PageNumber);
        Assert.Equal(1, errors[0].Location.LineNumber);
        Assert.Contains("Page 1", errors[0].Location.Description);
        Assert.Contains("Line 1", errors[0].Location.Description);
    }

    [Fact]
    public void Validate_RuleNameIsCorrect()
    {
        // Assert
        Assert.Equal("FontFamily", _rule.Name);
    }
}
