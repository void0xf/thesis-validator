using backend.Models;
using Backend.Models;
using ThesisValidator.Rules;
using backend.Services.Results;

namespace backend.Tests.Services;

public class ValidationResultFactoryTests
{
    [Fact]
    public void Create_UsesRuleCatalogDefaults()
    {
        var result = ValidationResultFactory.ForParagraph(
            "FontFamily",
            new UniversityConfig(),
            "Wrong font",
            2,
            "body");

        Assert.Equal("FontFamily", result.RuleName);
        Assert.Equal("Error", result.Severity);
        Assert.True(result.IsError);
        Assert.Equal("Formatting", result.Category);
        Assert.Equal(2, result.Location.Paragraph);
    }

    [Fact]
    public void Create_ConfiguredSeverityOverridesRuleDefaultAndExplicitSeverity()
    {
        var config = new UniversityConfig();
        config.Rules.Overrides["FontFamily"] = new RuleOverrideConfig
        {
            Severity = "Warning"
        };

        var result = ValidationResultFactory.Create(
            "FontFamily",
            config,
            "Wrong font",
            severity: ValidationSeverity.Error);

        Assert.Equal("Warning", result.Severity);
        Assert.False(result.IsError);
    }

    [Fact]
    public void IsErrorSetter_KeepsSeverityBackwardCompatible()
    {
        var result = new ValidationResult { IsError = false };

        Assert.Equal("Warning", result.Severity);

        result.IsError = true;

        Assert.Equal("Error", result.Severity);
    }

    [Fact]
    public void RuleCatalog_KeepsManualTocAsSelectableWarning()
    {
        var definition = RuleCatalog.GetDefinition("Manual table of contents");

        Assert.True(definition.Selectable);
        Assert.Equal("Warning", definition.DefaultSeverity);
        Assert.Equal("Structure", definition.Category);
    }

}
