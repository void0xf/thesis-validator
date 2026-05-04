using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Content;
using backend.Application.Validation;
using backend.Annotation;
using backend.Endpoints.Documents;
using backend.Endpoints.Documents.Responses;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Tests.Endpoints;

public class DocumentEndpointTests
{
    private const string ValidateDocumentMethodName = "ValidateDocument";
    private const string ValidateWithCommentsMethodName = "ValidateWithComments";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("[\"\"]")]
    public void ValidateDocument_WithoutSelectedRules_ReturnsBadRequest(string? rules)
    {
        var result = InvokeEndpoint(ValidateDocumentMethodName, rules);

        var problem = AssertBadRequest(result);
        Assert.Equal("No rules provided", problem.Title);
        Assert.Equal("Please include at least one validation rule in the rules form field", problem.Detail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("[\"\"]")]
    public void ValidateWithComments_WithoutSelectedRules_ReturnsBadRequest(string? rules)
    {
        var result = InvokeEndpoint(ValidateWithCommentsMethodName, rules);

        var problem = AssertBadRequest(result);
        Assert.Equal("No rules provided", problem.Title);
        Assert.Equal("Please include at least one validation rule in the rules form field", problem.Detail);
    }

    [Fact]
    public void ValidateDocument_WithInvalidRulesJson_ReturnsBadRequest()
    {
        var result = InvokeEndpoint(ValidateDocumentMethodName, "FontFamily");

        var problem = AssertBadRequest(result);
        Assert.Equal("Invalid rules", problem.Title);
        Assert.Equal("The rules form field must contain a JSON array of rule names", problem.Detail);
    }

    [Theory]
    [InlineData(ValidateDocumentMethodName)]
    [InlineData(ValidateWithCommentsMethodName)]
    public void ValidateEndpoints_WithUnknownSelectedRules_ReturnBadRequest(string methodName)
    {
        var result = InvokeEndpoint(
            methodName,
            "[\"MissingRule\"]",
            new TestValidationRule("FontFamily"));

        var problem = AssertBadRequest(result);
        Assert.Equal("Unknown validation rules", problem.Title);
        Assert.Contains("MissingRule", problem.Detail);
    }

    [Fact]
    public void GetAvailableRules_IncludesRuleDescriptions()
    {
        var result = InvokeGetAvailableRules(
            new TestValidationRule(
                "FontFamily",
                "Checks whether thesis text uses the configured font family."));

        var value = AssertOk(result);
        var rulesValue = value.GetType().GetProperty("Rules")?.GetValue(value);
        var rules = Assert.IsAssignableFrom<IEnumerable<object>>(rulesValue);
        var rule = Assert.Single(rules);

        Assert.Equal(
            "FontFamily",
            rule.GetType().GetProperty("Name")?.GetValue(rule));
        Assert.Equal(
            "Checks whether thesis text uses the configured font family.",
            rule.GetType().GetProperty("Description")?.GetValue(rule));
    }

    [Theory]
    [InlineData(ValidateDocumentMethodName)]
    [InlineData(ValidateWithCommentsMethodName)]
    public void ValidateEndpoints_WithUnreadableDocx_ReturnGenericBadRequest(string methodName)
    {
        var result = InvokeEndpoint(
            methodName,
            "[\"FontFamily\"]",
            new TestValidationRule("FontFamily"));

        var problem = AssertBadRequest(result);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal(
            "The uploaded document could not be processed. Make sure it is a valid .docx file.",
            problem.Detail);
    }

    [Fact]
    public void CreateValidationResponse_MapsValidationIssuesToResponseDtos()
    {
        var endpointResultsType = typeof(DocumentEndpoint).Assembly.GetType(
            "backend.Endpoints.Documents.DocumentEndpointResults");

        Assert.NotNull(endpointResultsType);

        var method = endpointResultsType.GetMethod(
            "CreateValidationResponse",
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);

        using var stream = new MemoryStream(new byte[12]);
        var request = CreateUploadRequest(new FormFile(stream, 0, stream.Length, "file", "thesis.docx"));
        var issues = new List<ValidationIssue>
        {
            new()
            {
                RuleName = "FontFamily",
                Message = "Wrong font",
                Category = RuleCategories.Formatting,
                Severity = RuleSeverity.Error,
                Location = new DocumentLocation
                {
                    Paragraph = 2,
                    Run = 1,
                    CharacterOffset = 4,
                    Length = 5,
                    Text = "Arial",
                    Section = "Introduction"
                }
            }
        };

        var response = Assert.IsType<DocumentValidationResponse>(
            method.Invoke(null, [request, issues]));
        var issue = Assert.Single(response.Results);

        Assert.Equal("thesis.docx", response.FileName);
        Assert.False(response.IsValid);
        Assert.Equal(1, response.TotalErrors);
        Assert.Equal(0, response.TotalWarnings);
        Assert.Equal("FontFamily", issue.RuleName);
        Assert.True(issue.IsError);
        Assert.Equal(RuleSeverity.Error.ToString(), issue.Severity);
        Assert.Equal("Introduction", issue.Location.Section);
    }

    private static IResult InvokeEndpoint(string methodName, string? rules, params IValidationRule[] availableRules)
    {
        var method = typeof(DocumentEndpoint).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(
            null,
            new object?[]
            {
                CreateDocxFile(),
                rules,
                CreateValidator(availableRules),
                NullLoggerFactory.Instance
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static IResult InvokeGetAvailableRules(params IValidationRule[] availableRules)
    {
        var method = typeof(DocumentEndpoint).GetMethod(
            "GetAvailableRules",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(
            null,
            new object?[]
            {
                CreateValidator(availableRules)
            });

        return Assert.IsAssignableFrom<IResult>(result);
    }

    private static IFormFile CreateDocxFile()
    {
        var stream = new MemoryStream(new byte[] { 1 });
        return new FormFile(stream, 0, stream.Length, "file", "thesis.docx");
    }

    private static object CreateUploadRequest(IFormFile file)
    {
        var requestType = typeof(DocumentEndpoint).Assembly.GetType(
            "backend.Endpoints.Documents.DocumentUploadRequest");

        Assert.NotNull(requestType);

        return Activator.CreateInstance(
            requestType,
            file,
            "thesis.docx",
            new List<string> { "FontFamily" })!;
    }

    private static object AssertOk(IResult result)
    {
        var resultType = result.GetType();
        var statusCode = resultType.GetProperty("StatusCode")?.GetValue(result);
        var value = resultType.GetProperty("Value")?.GetValue(result);

        Assert.Equal(StatusCodes.Status200OK, statusCode);
        Assert.NotNull(value);
        return value;
    }

    private static ProblemDetails AssertBadRequest(IResult result)
    {
        var resultType = result.GetType();
        var statusCode = resultType.GetProperty("StatusCode")?.GetValue(result);
        var value = resultType.GetProperty("Value")?.GetValue(result);

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        return Assert.IsType<ProblemDetails>(value);
    }

    private static ThesisValidationOrchestrator CreateValidator(params IValidationRule[] rules)
    {
        var configuration = new ConfigurationBuilder().Build();
        var policyResolver = new RulePolicyResolver(configuration);
        var optionsBinder = new RuleOptionsBinder(configuration);
        var resultComposer = new ValidationIssueComposer();

        return new ThesisValidationOrchestrator(
            new DocumentSession(),
            new DocumentContentAnalyzer(new DocumentSkipResolver(
                Options.Create(new ValidationSkippingOptions()))),
            new RuleRunner(rules, policyResolver, optionsBinder, resultComposer),
            new SectionContextResolver(),
            new AnnotationApplicator());
    }

    private sealed class TestValidationRule : ValidationRule<TestValidationRuleOptions>
    {
        private readonly string _name;
        private readonly string _description;

        public TestValidationRule(
            string name,
            string? description = null)
        {
            _name = name;
            _description = description ?? name;
        }

        public override RuleDescriptor Descriptor => new(
            Name: _name,
            DisplayName: _name,
            Description: _description,
            Category: RuleCategories.Formatting,
            DefaultAvailability: RuleAvailability.Available,
            DefaultSeverity: RuleSeverity.Error);

        public override IEnumerable<RuleProblem> Validate(
            RuleContext context,
            TestValidationRuleOptions options)
        {
            return [];
        }
    }

    private sealed class TestValidationRuleOptions : RuleOptionsBase;
}
