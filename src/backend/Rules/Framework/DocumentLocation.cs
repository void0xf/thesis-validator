namespace ThesisValidator.Rules;

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
    /// The text content at this location, usually truncated.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The nearest heading/section this issue falls under.
    /// </summary>
    public string Section { get; set; } = string.Empty;
}
