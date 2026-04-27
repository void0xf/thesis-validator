using backend.Models;
using backend.Services.Analysis;
using backend.Services.Comments;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Tests.Services;

public class ThesisValidatorServiceRuleSelectionTests
{
    [Fact]
    public void Validate_WithSelectedRules_RunsOnlySelectedRules()
    {
        var selectedRule = new RecordingRule("FontFamily");
        var unselectedRule = new RecordingRule("Grammar");
        var service = new ThesisValidatorService([selectedRule, unselectedRule]);
        using var stream = CreateDocxStream();

        service.Validate(stream, new UniversityConfig(), ["FontFamily"]);

        Assert.Equal(1, selectedRule.RunCount);
        Assert.Equal(0, unselectedRule.RunCount);
    }

    [Fact]
    public void Validate_SelectedRuleRunsWhenOnlySeverityIsConfigured()
    {
        var selectedRule = new RecordingRule("FontFamily");
        var service = new ThesisValidatorService([selectedRule]);
        using var stream = CreateDocxStream();
        var config = new UniversityConfig();
        config.Rules.Overrides["FontFamily"] = new RuleOverrideConfig
        {
            Severity = ValidationSeverity.Warning
        };

        service.Validate(stream, config, ["FontFamily"]);

        Assert.Equal(1, selectedRule.RunCount);
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
            return [];
        }
    }
}
