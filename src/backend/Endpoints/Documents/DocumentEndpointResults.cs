using backend.Application.Validation;
using backend.Endpoints.Documents.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Endpoints.Documents;

internal static class DocumentEndpointResults
{
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private const string LoggerCategory = "backend.Endpoints.Documents.DocumentEndpoint";

    public static DocumentValidationResponse CreateValidationResponse(
        DocumentUploadRequest request,
        IReadOnlyList<ValidationIssue> results)
    {
        return new DocumentValidationResponse
        {
            FileName = request.FileName,
            FileSize = request.File.Length,
            ValidatedAt = DateTime.UtcNow,
            IsValid = !results.Any(r => r.IsError),
            TotalErrors = results.Count(r => r.IsError),
            TotalWarnings = results.Count(r => !r.IsError),
            Results = results
                .Select(ValidationIssueResponse.From)
                .ToList()
        };
    }

    public static IResult NoFileProvided()
    {
        return BadRequest(
            "No file provided",
            "Please upload a .docx file");
    }

    public static IResult InvalidFileType()
    {
        return BadRequest(
            "Invalid file type",
            "Only .docx files are supported");
    }

    public static IResult MissingRules()
    {
        return BadRequest(
            "No rules provided",
            "Please include at least one validation rule in the rules form field");
    }

    public static IResult InvalidRulesJson()
    {
        return BadRequest(
            "Invalid rules",
            "The rules form field must contain a JSON array of rule names");
    }

    public static IResult UnknownRules(IReadOnlyCollection<string> unknownRules)
    {
        var problemDetails = CreateBadRequestProblem(
            "Unknown validation rules",
            $"Unknown validation rules: {string.Join(", ", unknownRules)}. Use /api/documents/rules to retrieve available rules.");

        problemDetails.Extensions["unknownRules"] = unknownRules.ToArray();
        return Results.BadRequest(problemDetails);
    }

    public static IResult InvalidDocument(Exception exception, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LoggerCategory);
        logger.LogWarning(exception, "Failed to process uploaded DOCX document.");

        return BadRequest(
            "Validation failed",
            "The uploaded document could not be processed. Make sure it is a valid .docx file.");
    }

    public static IResult UnexpectedValidationFailure(Exception exception, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LoggerCategory);
        logger.LogError(exception, "Unexpected error while validating uploaded DOCX document.");

        return Results.Problem(
            title: "Validation failed",
            detail: "An unexpected error occurred while processing the document.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    public static string GetAnnotatedFileName(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        return $"{(string.IsNullOrWhiteSpace(baseName) ? "document" : baseName)}_annotated.docx";
    }

    private static IResult BadRequest(string title, string detail)
    {
        return Results.BadRequest(CreateBadRequestProblem(title, detail));
    }

    private static ProblemDetails CreateBadRequestProblem(string title, string detail)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        };
    }
}
