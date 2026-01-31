using backend.Models;
using backend.Services;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Endpoints;

public static class DocumentEndpoint
{
    public static void MapDocumentEndpoint(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .DisableAntiforgery();

        group.MapPost("/validate", ValidateDocument)
            .WithName("ValidateDocument")
            .WithSummary("Validate a thesis document")
            .WithDescription("Uploads a DOCX file and validates it against university formatting rules. Returns JSON with validation results.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DocumentValidationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/validate-with-comments", ValidateWithComments)
            .WithName("ValidateWithComments")
            .WithSummary("Validate and annotate a thesis document")
            .WithDescription("Uploads a DOCX file, validates it, and returns an annotated version with comments marking each error.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/rules", GetAvailableRules)
            .WithName("GetAvailableRules")
            .WithSummary("Get available validation rules")
            .WithDescription("Returns a list of all available validation rules");

        group.MapGet("/health", HealthCheck)
            .WithName("DocumentServiceHealth")
            .WithSummary("Health check endpoint");
    }

    private static IResult ValidateDocument(
        IFormFile? file,
        ThesisValidatorService thesisValidatorService,
        IOptions<UniversityConfig> universityConfigOptions)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "No file provided",
                Detail = "Please upload a .docx file",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid file type",
                Detail = "Only .docx files are supported",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var config = universityConfigOptions.Value;
            var results = thesisValidatorService.Validate(stream, config).ToList();

            var response = new DocumentValidationResponse
            {
                FileName = file.FileName,
                FileSize = file.Length,
                ValidatedAt = DateTime.UtcNow,
                IsValid = !results.Any(r => r.IsError),
                TotalErrors = results.Count(r => r.IsError),
                TotalWarnings = results.Count(r => !r.IsError),
                Results = results,
                ConfigUsed = config.Name
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = $"Failed to process document: {ex.Message}",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static IResult ValidateWithComments(
        IFormFile? file,
        ThesisValidatorService thesisValidatorService,
        IOptions<UniversityConfig> universityConfigOptions)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "No file provided",
                Detail = "Please upload a .docx file",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid file type",
                Detail = "Only .docx files are supported",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var config = universityConfigOptions.Value;
            var (results, annotatedDocument) = thesisValidatorService.ValidateWithComments(stream, config);

            var resultsList = results.ToList();
            var errorCount = resultsList.Count(r => r.IsError);
            var warningCount = resultsList.Count(r => !r.IsError);

            var outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "output");
            Directory.CreateDirectory(outputFolder);

            var outputFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_validated_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            var outputPath = Path.Combine(outputFolder, outputFileName);

            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                annotatedDocument.CopyTo(fileStream);
            }

            annotatedDocument.Dispose();

            return Results.Ok(new
            {
                FileName = file.FileName,
                OutputPath = outputPath,
                ValidatedAt = DateTime.UtcNow,
                IsValid = errorCount == 0,
                TotalErrors = errorCount,
                TotalWarnings = warningCount,
                Results = resultsList,
                ConfigUsed = config.Name
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = $"Failed to process document: {ex.Message}",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static IResult GetAvailableRules(IEnumerable<ThesisValidator.Rules.IValidationRule> rules)
    {
        var ruleList = rules.Select(r => new { r.Name }).ToList();
        return Results.Ok(new { Rules = ruleList, Count = ruleList.Count });
    }

    private static IResult HealthCheck()
    {
        return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}

public class DocumentValidationResponse
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime ValidatedAt { get; set; }
    public bool IsValid { get; set; }
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public string ConfigUsed { get; set; } = string.Empty;
    public List<ValidationResult> Results { get; set; } = new();
}