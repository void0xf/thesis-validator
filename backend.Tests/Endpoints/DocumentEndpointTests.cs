using System.Reflection;
using backend.Models;
using backend.Endpoints;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
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
                null,
                null,
                new ThesisValidatorService(availableRules),
                Options.Create(new UniversityConfig()),
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

    private sealed class TestValidationRule : IValidationRule
    {
        public TestValidationRule(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IEnumerable<ValidationResult> Validate(
            WordprocessingDocument doc,
            UniversityConfig config,
            DocumentCommentService? documentCommentService = null)
        {
            return Array.Empty<ValidationResult>();
        }
    }
}
