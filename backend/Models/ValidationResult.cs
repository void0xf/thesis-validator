using System.Text.Json.Serialization;

namespace backend.Models;

public static class ValidationSeverity
{
    public const string Info = "Info";
    public const string Error = "Error";
    public const string Warning = "Warning";

    public static string Normalize(string? severity)
    {
        if (string.Equals(severity, Info, StringComparison.OrdinalIgnoreCase))
            return Info;

        if (string.Equals(severity, Warning, StringComparison.OrdinalIgnoreCase))
            return Warning;

        return Error;
    }
}

public enum ParagraphIndexKind
{
    Descendant,
    BodyElement
}

public class ValidationResult
{
    private string _severity = ValidationSeverity.Error;

    public string RuleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool IsError
    {
        get => string.Equals(Severity, ValidationSeverity.Error, StringComparison.OrdinalIgnoreCase);
        set => Severity = value ? ValidationSeverity.Error : ValidationSeverity.Warning;
    }

    public string Severity
    {
        get => _severity;
        set => _severity = ValidationSeverity.Normalize(value);
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }

    [JsonIgnore]
    public ParagraphIndexKind ParagraphIndexKind { get; set; } = ParagraphIndexKind.Descendant;

    public DocumentLocation Location { get; set; } = new();
}

/// <summary>
/// Represents the exact location of a validation issue within a document.
/// </summary>
public class DocumentLocation
{

    /// <summary>
    /// 1-based paragraph index in the document body.
    /// </summary>
    public int Paragraph { get; set; }

    /// <summary>
    /// 1-based run index within the paragraph.
    /// </summary>
    public int Run { get; set; }

    /// <summary>
    /// Character offset from the start of the paragraph.
    /// </summary>
    public int CharacterOffset { get; set; }

    /// <summary>
    /// Length of the affected text.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// The text content at this location (may be truncated).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The nearest heading / section this issue falls under (e.g. "1.2 Background").
    /// Populated automatically after validation.
    /// </summary>
    public string Section { get; set; } = string.Empty;


}

public class HeadingInfo
{
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
}
