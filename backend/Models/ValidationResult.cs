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
    private const int ApproximateLinesPerPage = 30;
    private int _pageNumber;
    private int _lineNumber;

    /// <summary>
    /// Approximate page number (1-based). Calculated based on page breaks and paragraph count.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber > 0 ? _pageNumber : EstimatePageNumber();
        set => _pageNumber = value;
    }

    /// <summary>
    /// Approximate line number within the page (1-based).
    /// </summary>
    public int LineNumber
    {
        get => _lineNumber > 0 ? _lineNumber : EstimateLineNumber();
        set => _lineNumber = value;
    }

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

    /// <summary>
    /// Human-readable description of the location.
    /// </summary>
    public string Description => string.IsNullOrEmpty(Section)
        ? DescribeParagraphLocation()
        : Section;

    public override string ToString() => Description;

    private int EstimatePageNumber()
    {
        return Paragraph <= 0
            ? 0
            : ((Paragraph - 1) / ApproximateLinesPerPage) + 1;
    }

    private int EstimateLineNumber()
    {
        return Paragraph <= 0
            ? 0
            : ((Paragraph - 1) % ApproximateLinesPerPage) + 1;
    }

    private string DescribeParagraphLocation()
    {
        return Paragraph <= 0
            ? "Document"
            : $"Page {PageNumber}, Line {LineNumber}, Paragraph {Paragraph}";
    }
}

public class HeadingInfo
{
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
}
