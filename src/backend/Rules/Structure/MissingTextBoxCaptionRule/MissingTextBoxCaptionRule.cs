using backend.DocumentProcessing.Content;
using backend.DocumentProcessing.Skipping;
using backend.DocumentProcessing.TextBoxes;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using ThesisValidator.Rules;

namespace backend.Rules;

/// <summary>
/// Detects text boxes that do not have an adjacent caption.
/// </summary>
public sealed class MissingTextBoxCaptionRule : ValidationRule<MissingTextBoxCaptionRuleOptions>
{
    public const string RuleId = nameof(MissingTextBoxCaptionRule);

    public override RuleDescriptor Descriptor => new(
        Name: RuleId,
        DisplayName: "Missing Text Box Captions",
        Description: "Finds text boxes without an adjacent caption.",
        Category: RuleCategories.Structure,
        DefaultAvailability: RuleAvailability.Available,
        DefaultSeverity: RuleSeverity.Warning);

    public override IEnumerable<RuleProblem> Validate(
        RuleContext context,
        MissingTextBoxCaptionRuleOptions options)
    {
        var body = context.RawDocument.MainDocumentPart?.Document.Body;
        if (body is null)
            yield break;

        var children = body.ChildElements;
        var paragraphIndex = 0;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            if (TextBoxContentDetector.IsInsideTextBoxOrDrawingText(paragraph))
                continue;

            paragraphIndex++;

            if (!TextBoxContentDetector.ContainsTextBoxContent(paragraph))
                continue;

            var bodyChildIndex = GetBodyChildIndex(paragraph, body);
            if (bodyChildIndex is null)
                continue;

            if (TextBoxCaptionDetection.HasAdjacentCaption(
                    context.RawDocument,
                    children,
                    bodyChildIndex.Value))
            {
                continue;
            }

            var preview = TextExtractor.GetPreview(paragraph, skipTextBoxes: false, maxLength: 60);
            yield return new RuleProblem(
                "Text box has no caption - add a caption above or below the text box.",
                new DocumentLocation
                {
                    Paragraph = paragraphIndex,
                    Text = string.IsNullOrWhiteSpace(preview) ? "[Text box]" : preview
                },
                ParagraphIndexKind.BodyElement,
                new ParagraphAnnotationTarget(paragraph));
        }
    }

    private static int? GetBodyChildIndex(OpenXmlElement element, Body body)
    {
        var bodyChild = GetBodyChildAncestor(element, body);
        if (bodyChild is null)
            return null;

        for (var index = 0; index < body.ChildElements.Count; index++)
        {
            if (ReferenceEquals(body.ChildElements[index], bodyChild))
                return index;
        }

        return null;
    }

    private static OpenXmlElement? GetBodyChildAncestor(OpenXmlElement element, Body body)
    {
        OpenXmlElement? current = element;
        while (current?.Parent is not null && current.Parent != body)
        {
            current = current.Parent;
        }

        return current?.Parent == body ? current : null;
    }
}
