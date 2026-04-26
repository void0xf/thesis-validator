using System.Globalization;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Skipping;

public sealed class TextBoxSkipRule : ISkipRule
{
    private const string WordprocessingNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string DrawingNamespace = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private const string VmlNamespace = "urn:schemas-microsoft-com:vml";

    public SkipDecision ShouldSkipParagraph(
        DocumentFormat.OpenXml.Packaging.WordprocessingDocument doc,
        Paragraph paragraph,
        UniversityConfig config,
        SkipContext context)
    {
        return ShouldSkipParagraph(paragraph, config)
            ? SkipDecision.Skip(SkipReason.TextBox, "Paragraph is inside or only contains text-box/drawing text.")
            : SkipDecision.Include;
    }

    public SkipDecision ShouldSkipRun(
        DocumentFormat.OpenXml.Packaging.WordprocessingDocument doc,
        Paragraph paragraph,
        Run run,
        UniversityConfig config,
        SkipContext context)
    {
        return SkipDecisionService.ShouldSkipTextBoxes(config) && IsInsideTextBoxOrDrawingText(run)
            ? SkipDecision.Skip(SkipReason.TextBox, "Run is inside text-box/drawing text.")
            : SkipDecision.Include;
    }

    public SkipDecision ShouldSkipElement(
        OpenXmlElement element,
        UniversityConfig config,
        SkipContext context)
    {
        return SkipDecisionService.ShouldSkipTextBoxes(config) && ContainsTextBoxContent(element)
            ? SkipDecision.Skip(SkipReason.TextBox, "Element contains text-box/drawing text.")
            : SkipDecision.Include;
    }

    public static bool ShouldSkipParagraph(Paragraph paragraph, UniversityConfig config)
    {
        return SkipDecisionService.ShouldSkipTextBoxes(config)
            && (IsInsideTextBoxOrDrawingText(paragraph) || IsTextBoxOnlyParagraph(paragraph));
    }

    public static bool ContainsTextBoxContent(OpenXmlElement element)
    {
        return ContainsElement(element, IsTextBoxContentElement);
    }

    public static bool IsInsideTextBoxOrDrawingText(OpenXmlElement element)
    {
        OpenXmlElement? current = element;
        while (current is not null)
        {
            if (IsTextBoxOrDrawingTextElement(current))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private static bool IsTextBoxOnlyParagraph(Paragraph paragraph)
    {
        return paragraph.Descendants<Text>().Any(IsInsideTextBoxOrDrawingText)
            && !HasMeaningfulContent(GetParagraphTextWithoutTextBoxes(paragraph));
    }

    private static string GetParagraphTextWithoutTextBoxes(Paragraph paragraph)
    {
        return string.Concat(paragraph
            .Descendants<Text>()
            .Where(text => !IsInsideTextBoxOrDrawingText(text))
            .Select(text => text.Text));
    }

    private static bool HasMeaningfulContent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
                continue;

            if (char.GetUnicodeCategory(ch) == UnicodeCategory.Format)
                continue;

            return true;
        }

        return false;
    }

    private static bool IsTextBoxOrDrawingTextElement(OpenXmlElement element)
    {
        return IsWordprocessingElement(element, "drawing")
            || IsWordprocessingElement(element, "pict")
            || IsWordprocessingElement(element, "txbxContent")
            || IsVmlElement(element, "shape")
            || IsVmlElement(element, "textbox")
            || IsDrawingElement(element, "txBody");
    }

    private static bool IsTextBoxContentElement(OpenXmlElement element)
    {
        return IsWordprocessingElement(element, "txbxContent")
            || IsVmlElement(element, "textbox")
            || IsDrawingElement(element, "txBody");
    }

    private static bool ContainsElement(OpenXmlElement element, Func<OpenXmlElement, bool> predicate)
    {
        if (predicate(element))
            return true;

        foreach (var child in element.ChildElements)
        {
            if (ContainsElement(child, predicate))
                return true;
        }

        return false;
    }

    private static bool IsWordprocessingElement(OpenXmlElement element, string localName)
    {
        return element.LocalName == localName && element.NamespaceUri == WordprocessingNamespace;
    }

    private static bool IsDrawingElement(OpenXmlElement element, string localName)
    {
        return element.LocalName == localName && element.NamespaceUri == DrawingNamespace;
    }

    private static bool IsVmlElement(OpenXmlElement element, string localName)
    {
        return element.LocalName == localName && element.NamespaceUri == VmlNamespace;
    }
}
