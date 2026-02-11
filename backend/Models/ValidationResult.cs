namespace backend.Models;

public class ValidationResult
{
    public string RuleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public DocumentLocation Location { get; set; } = new();
}

/// <summary>
/// Represents the exact location of a validation issue within a document.
/// </summary>
public class DocumentLocation
{
    /// <summary>
    /// Approximate page number (1-based). Calculated based on page breaks and paragraph count.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Approximate line number within the page (1-based).
    /// </summary>
    public int LineNumber { get; set; }

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
    /// Human-readable description of the location.
    /// </summary>
    public string Description => $"Page {PageNumber}, Line {LineNumber} (Paragraph {Paragraph})";

    public override string ToString() => Description;
}

public class HeadingInfo
{
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
}