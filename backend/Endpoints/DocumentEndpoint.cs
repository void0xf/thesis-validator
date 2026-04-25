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
        [FromForm] bool? skipBeforeTableOfContents,
        [FromForm] bool? skipTextBoxes,
        ThesisValidatorService thesisValidatorService,
        IOptions<UniversityConfig> universityConfigOptions,
        ILoggerFactory loggerFactory)
    {
        if (!DocumentUploadRequestValidator.TryValidate(file, rules, skipBeforeTableOfContents, skipTextBoxes, thesisValidatorService, out var request, out var error))
        {
            return error!;
        }

        try
        {
            using var stream = request!.File.OpenReadStream();
            var config = CreateRequestConfig(universityConfigOptions.Value, request);
            var (validationResults, headings) = thesisValidatorService.Validate(stream, config, request.SelectedRules);
            var results = validationResults.ToList();

            var response = DocumentEndpointResults.CreateValidationResponse(request, config, results, headings);
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
        [FromForm] bool? skipBeforeTableOfContents,
        [FromForm] bool? skipTextBoxes,
        ThesisValidatorService thesisValidatorService,
        IOptions<UniversityConfig> universityConfigOptions,
        ILoggerFactory loggerFactory)
    {
        if (!DocumentUploadRequestValidator.TryValidate(file, rules, skipBeforeTableOfContents, skipTextBoxes, thesisValidatorService, out var request, out var error))
        {
            return error!;
        }

        try
        {
            using var stream = request!.File.OpenReadStream();
            var config = CreateRequestConfig(universityConfigOptions.Value, request);
            var (_, annotatedDocument) = thesisValidatorService.ValidateWithComments(stream, config, request.SelectedRules);

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

    private static IResult GetAvailableRules(ThesisValidatorService thesisValidatorService)
    {
        var ruleList = thesisValidatorService.GetAvailableRuleNames()
            .Select(name => new { Name = name })
            .ToList();

        return Results.Ok(new { Rules = ruleList, Count = ruleList.Count });
    }

    private static IResult HealthCheck()
    {
        return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }

    private static UniversityConfig CreateRequestConfig(
        UniversityConfig baseConfig,
        DocumentUploadRequest request)
    {
        return new UniversityConfig
        {
            Name = baseConfig.Name,
            CheckGrammar = baseConfig.CheckGrammar,
            Language = baseConfig.Language,
            Formatting = new FormattingConfig
            {
                CheckTableOfContents = baseConfig.Formatting.CheckTableOfContents,
                SkipBeforeTableOfContents = request.SkipBeforeTableOfContents,
                SkipTextBoxes = request.SkipTextBoxes ?? baseConfig.Formatting.SkipTextBoxes,
                SkipTableOfContentsContent = baseConfig.Formatting.SkipTableOfContentsContent,
                Font = new FontConfig
                {
                    FontFamily = baseConfig.Formatting.Font.FontFamily,
                    FontSize = baseConfig.Formatting.Font.FontSize
                },
                Layout = new LayoutConfig
                {
                    MarginLeft = baseConfig.Formatting.Layout.MarginLeft,
                    MarginRight = baseConfig.Formatting.Layout.MarginRight,
                    RequiredIndentCm = baseConfig.Formatting.Layout.RequiredIndentCm,
                    ParagraphSpacingRule = baseConfig.Formatting.Layout.ParagraphSpacingRule.ToList()
                }
            }
        };
    }
}
