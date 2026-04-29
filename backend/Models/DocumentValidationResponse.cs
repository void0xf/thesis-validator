namespace backend.Models;

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
