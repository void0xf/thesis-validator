using backend.Models;
using backend.RuleOptions;
using backend.Services.Comments;
using backend.Services.Extraction;
using backend.Services.Results;
using backend.Services.Rules;
using backend.Services.Structure;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Validates that an existing figure caption is placed below its figure.
/// </summary>
public class FigureCaptionPositionRule : IValidationRule
{
    public const string RuleId = nameof(FigureCaptionPositionRule);

    private readonly IRuleConfigurationService _ruleConfigurationService;

    public FigureCaptionPositionRule(
        IRuleConfigurationService? ruleConfigurationService = null,
        IOptions<FigureCaptionPositionRuleOptions>? options = null)
    {
        var figureCaptionPositionOptions = options ?? Options.Create(new FigureCaptionPositionRuleOptions());

        _ruleConfigurationService = ruleConfigurationService
            ?? new RuleConfigurationService(
                Options.Create(new EmptySectionStructureRuleOptions()),
                figureCaptionPositionOptions: figureCaptionPositionOptions);
    }

    // The validator infrastructure uses this identifier to label results
    // and to look up rule metadata/configuration.
    public string Name => RuleId;

    public IEnumerable<ValidationResult> Validate(
        // OpenXML SDK representation of the .docx package. The rule reads
        // paragraphs, drawings, styles, and other WordprocessingML content from here.
        WordprocessingDocument doc,
        // University-specific validation settings. This is passed through to
        // helper services so they can honor skip rules and severity settings.
        UniversityConfig config,
        // Optional helper that can inject a real Word comment into the offending
        // paragraph, so the issue is visible inside Microsoft Word as well.
        DocumentCommentService? commentService = null)
    {
        if (!_ruleConfigurationService.IsRuleAvailable(Name))
            return [];

        // We collect every detected violation here and return them at the end.
        var errors = new List<ValidationResult>();

        // FigureCaptionDetector.AssociateFiguresWithCaptions(doc, config):
        // 1. Walks the document paragraphs in document order, including nested
        //    paragraphs such as those inside table cells.
        // 2. Uses FigureDetectionService to find paragraphs containing visual figures.
        // 3. Searches nearby paragraphs for a matching caption.
        // 4. Returns one FigureCaptionAssociation per figure, describing:
        //    - the figure paragraph,
        //    - the caption paragraph if found,
        //    - the relation (Below / Above / SeparatedBelow / None).
        foreach (var association in FigureCaptionDetector.AssociateFiguresWithCaptions(
                     doc,
                     config,
                     requireStructuredCaption: true))
        {
            // If no caption was found, this rule does nothing because missing captions
            // are handled by a different rule.
            //
            // If IsCaptionBelowFigure is true, the relation is exactly "Below", which
            // means the caption is in the correct position and there is nothing to report.
            if (association.Caption is null || association.IsCaptionBelowFigure)
                continue;

            // association.Caption.Text was already extracted from the caption paragraph.
            // Truncate(...) shortens it to at most 50 characters so UI/result payloads
            // show a compact preview instead of the full caption text.
            var preview = TextExtractionService.Truncate(association.Caption.Text, 50);

            // The message depends on how the detector classified the caption:
            // - Above: caption appears before the figure.
            // - SeparatedBelow: caption is below the figure, but not directly below;
            //   some other non-empty/intervening content was found in between.
            var message = association.Relation == FigureCaptionRelationKind.Above
                ? "Figure caption should be placed below the figure."
                : "Figure caption should be placed directly below the figure without intervening content.";

            // ValidationResultFactory.ForParagraph(...) creates a standardized
            // ValidationResult object with:
            // - the current rule id,
            // - severity/category resolved from config + rule catalog,
            // - a DocumentLocation pointing at the caption paragraph,
            // - the preview text shown to the user.
            //
            // association.Caption.ParagraphIndex is the 1-based descendant paragraph index
            // captured by FigureCaptionDetector while scanning the document.
            //
            // ParagraphIndexKind.Descendant tells downstream consumers that this index
            // refers to the full document-order descendant paragraph traversal.
            var result = ValidationResultFactory.ForParagraph(
                Name,
                config,
                message,
                association.Caption.ParagraphIndex,
                preview,
                ParagraphIndexKind.Descendant);
            result.Severity = _ruleConfigurationService.ResolveSeverity(Name, config);
            errors.Add(result);

            // If commentService is not null, add a Word comment directly to the caption
            // paragraph. Under the hood, AddCommentToParagraph(...) creates/reuses the
            // comments part in the .docx package and inserts comment markers so Word can
            // display the validation message inline in the document.
            commentService?.AddCommentToParagraph(doc, association.Caption.Paragraph, message);
        }

        // Return every caption-position issue found for this document.
        return errors;
    }
}
