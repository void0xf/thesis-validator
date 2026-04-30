using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using backend.DocumentProcessing.Documents;
using backend.DocumentProcessing.Context;
using backend.DocumentProcessing.Content;
using backend.Application.Validation;
using backend.Annotation;
using System.Reflection;
using backend.Models;
using backend.Endpoints;
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

    private static IFormFile CreateDocxFile()
    {
        var stream = new MemoryStream(new byte[] { 1 });
        return new FormFile(stream, 0, stream.Length, "file", "thesis.docx");
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
        var resultComposer = new ValidationResultComposer();

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

        public TestValidationRule(string name)
        {
            _name = name;
        }

        public override RuleDescriptor Descriptor => new(
            Name: _name,
            DisplayName: _name,
            Description: _name,
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
