using System.Text.Json.Serialization;
using backend.Application.Validation;
using ThesisValidator.Rules;

namespace backend.Endpoints.Documents.Responses;

public sealed class ValidationIssueResponse
{
    public string RuleName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsError => string.Equals(Severity, RuleSeverity.Error.ToString(), StringComparison.OrdinalIgnoreCase);

    public string Severity { get; set; } = RuleSeverity.Error.ToString();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }

    public DocumentLocationResponse Location { get; set; } = new();

    public static ValidationIssueResponse From(ValidationIssue issue)
    {
        return new ValidationIssueResponse
        {
            RuleName = issue.RuleName,
            Message = issue.Message,
            Severity = issue.Severity.ToString(),
            Category = issue.Category,
            Location = DocumentLocationResponse.From(issue.Location)
        };
    }
}
