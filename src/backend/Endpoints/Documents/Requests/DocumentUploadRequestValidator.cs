using ThesisValidationOrchestrator = backend.Application.Validation.ThesisValidator;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace backend.Endpoints.Documents;

internal static class DocumentUploadRequestValidator
{
    public static bool TryValidate(
        IFormFile? file,
        string? rules,
        ThesisValidationOrchestrator validator,
        out DocumentUploadRequest? request,
        out IResult? error)
    {
        request = null;

        error = ValidateUploadedFile(file, out var fileName);
        if (error is not null)
        {
            return false;
        }

        error = TryDeserializeRequiredRules(rules, out var selectedRules);
        if (error is not null)
        {
            return false;
        }

        var unknownRules = validator.GetUnknownRuleNames(selectedRules);
        if (unknownRules.Count > 0)
        {
            error = DocumentEndpointResults.UnknownRules(unknownRules);
            return false;
        }

        request = new DocumentUploadRequest(file!, fileName, selectedRules);
        return true;
    }

    private static IResult? ValidateUploadedFile(IFormFile? file, out string fileName)
    {
        fileName = string.Empty;

        if (file is null || file.Length == 0)
        {
            return DocumentEndpointResults.NoFileProvided();
        }

        fileName = GetSubmittedFileName(file);
        if (string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(Path.GetExtension(fileName), ".docx", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentEndpointResults.InvalidFileType();
        }

        return null;
    }

    private static IResult? TryDeserializeRequiredRules(string? rules, out List<string> selectedRules)
    {
        selectedRules = new List<string>();

        if (string.IsNullOrWhiteSpace(rules))
        {
            return DocumentEndpointResults.MissingRules();
        }

        List<string?>? parsedRules;
        try
        {
            parsedRules = JsonSerializer.Deserialize<List<string?>>(rules);
        }
        catch (JsonException)
        {
            return DocumentEndpointResults.InvalidRulesJson();
        }

        if (parsedRules is null)
        {
            return DocumentEndpointResults.MissingRules();
        }

        var uniqueRuleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in parsedRules)
        {
            if (string.IsNullOrWhiteSpace(rule))
            {
                continue;
            }

            var ruleName = rule.Trim();
            if (uniqueRuleNames.Add(ruleName))
            {
                selectedRules.Add(ruleName);
            }
        }

        return selectedRules.Count == 0
            ? DocumentEndpointResults.MissingRules()
            : null;
    }

    private static string GetSubmittedFileName(IFormFile file)
    {
        return Path.GetFileName(file.FileName.Replace('\\', '/'));
    }
}
