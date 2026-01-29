using Backend.Models;
using DocumentFormat.OpenXml.Packaging;

namespace backend.Models;

public class ValidationResult
{
    public string RuleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public string Location { get; set; } = string.Empty;
}