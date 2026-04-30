using backend.Models;
using backend.ModernServices;
using Microsoft.AspNetCore.Mvc;

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
            .WithDescription("Uploads a DOCX file and validates it against selected university formatting rules. Requires a JSON array of rule names in the rules form field.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DocumentValidationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/validate-with-comments", ValidateWithComments)
            .WithName("ValidateWithComments")
            .WithSummary("Validate and annotate a thesis document")
            .WithDescription("Uploads a DOCX file, validates it against selected rules, and returns an annotated version with comments marking each error. Requires a JSON array of rule names in the rules form field.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status200OK, contentType: DocumentEndpointResults.DocxContentType)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

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
        [FromForm] string? rules,
        ModernThesisValidatorService modernValidator,
        ILoggerFactory loggerFactory)
    {
        if (!DocumentUploadRequestValidator
                .TryValidate(
                    file,
                    rules,
                    modernValidator, out var request, out var error))
        {
            return error!;
        }

        try
        {
            using var stream = request!.File.OpenReadStream();
            var validationResults = modernValidator.Validate(stream, request.SelectedRules);
            var results = validationResults.ToList();

            var response = DocumentEndpointResults
                .CreateValidationResponse(
                    request, results);

            return Results.Ok(response);
        }
        catch (InvalidThesisDocumentException ex)
        {
            return DocumentEndpointResults.InvalidDocument(ex, loggerFactory);
        }
        catch (Exception ex)
        {
            return DocumentEndpointResults.UnexpectedValidationFailure(ex, loggerFactory);
        }
    }

    private static IResult ValidateWithComments(
        IFormFile? file,
        [FromForm] string? rules,
        ModernThesisValidatorService modernValidator,
        ILoggerFactory loggerFactory)
    {
        if (!DocumentUploadRequestValidator.TryValidate(file, rules, modernValidator, out var request, out var error))
        {
            return error!;
        }

        try
        {
            using var stream = request!.File.OpenReadStream();
            var (_, annotatedDocument) = modernValidator.ValidateWithComments(stream, request.SelectedRules);

            var outputFileName = DocumentEndpointResults.GetAnnotatedFileName(request.FileName);

            return Results.File(
                annotatedDocument,
                contentType: DocumentEndpointResults.DocxContentType,
                fileDownloadName: outputFileName
            );
        }
        catch (InvalidThesisDocumentException ex)
        {
            return DocumentEndpointResults.InvalidDocument(ex, loggerFactory);
        }
        catch (Exception ex)
        {
            return DocumentEndpointResults.UnexpectedValidationFailure(ex, loggerFactory);
        }
    }

    private static IResult GetAvailableRules(
        ModernThesisValidatorService modernValidator)
    {
        var ruleList = modernValidator.GetAvailableRules()
                .Select(rule => new
                {
                    Name = rule.Id,
                    rule.DisplayName,
                    rule.Category,
                    rule.DefaultSeverity,
                    rule.Enabled,
                    rule.Selectable
                })
                .ToList();

        return Results.Ok(new { Rules = ruleList, Count = ruleList.Count });

    }

    private static IResult HealthCheck()
    {
        return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }

}
