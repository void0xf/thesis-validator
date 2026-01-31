using backend.Models;
using backend.Services;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;

namespace ThesisValidator.Rules;

public interface IValidationRule
{
    /// <summary>
    /// The unique ID of this rule (e.g., "Formatting.Font").
    /// Matches the config section this rule validates.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Runs the validation logic.
    /// </summary>
    /// <param name="doc">The OpenXML Word document.</param>
    /// <param name="config">The full configuration object.</param>
    /// <param name="documentCommentService">Optional comment service to annotate the document.</param>
    /// <returns>A list of errors (or empty list if valid).</returns>
    IEnumerable<ValidationResult> Validate(WordprocessingDocument doc, UniversityConfig config, DocumentCommentService? documentCommentService = null);
}