namespace backend.Endpoints.Documents.Responses;

public sealed class DocumentValidationResponse
{
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime ValidatedAt { get; set; }

    public bool IsValid { get; set; }

    public int TotalErrors { get; set; }

    public int TotalWarnings { get; set; }

    public string ConfigUsed { get; set; } = string.Empty;

    public List<ValidationIssueResponse> Results { get; set; } = [];
}
