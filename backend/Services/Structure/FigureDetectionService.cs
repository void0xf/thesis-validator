using Backend.Models;
using backend.Services.Skipping;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services.Structure;

public static class FigureDetectionService
{
    public static bool ContainsImage(Paragraph paragraph, UniversityConfig config)
    {
        return ContainsFigureCandidate(paragraph, config);
    }

    public static bool ContainsFigureCandidate(Paragraph paragraph, UniversityConfig config)
    {
        if (paragraph.Descendants<Drawing>().Any(drawing =>
                IsFigureObject(drawing, config)))
        {
            return true;
        }

        if (paragraph.Descendants<Picture>().Any(picture =>
                IsFigureObject(picture, config)))
        {
            return true;
        }

        return false;
    }

    private static bool IsFigureObject(OpenXmlElement element, UniversityConfig config)
    {
        var hasVisualContent = ContainsVisualFigureContent(element);
        if (!hasVisualContent)
            return false;

        if (!SkipDecisionService.ShouldSkipTextBoxes(config))
            return true;

        return !TextBoxSkipRule.ContainsTextBoxContent(element)
            || ContainsVisualContentOutsideTextBox(element);
    }

    private static bool ContainsVisualContentOutsideTextBox(OpenXmlElement element)
    {
        return DescendantsAndSelf(element).Any(candidate =>
            !IsInsideTextBoxContent(candidate)
            && IsVisualFigureElement(candidate));
    }

    private static bool ContainsVisualFigureContent(OpenXmlElement element)
    {
        return DescendantsAndSelf(element).Any(IsVisualFigureElement);
    }

    private static bool IsVisualFigureElement(OpenXmlElement element)
    {
        if (IsElement(element, "http://schemas.openxmlformats.org/drawingml/2006/picture", "pic")
            || IsElement(element, "http://schemas.openxmlformats.org/drawingml/2006/main", "blip")
            || IsElement(element, "http://schemas.openxmlformats.org/drawingml/2006/chart", "chart")
            || IsElement(element, "urn:schemas-microsoft-com:vml", "imagedata")
            || IsElement(element, "urn:schemas-microsoft-com:office:office", "OLEObject")
            || IsElement(element, "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "object"))
        {
            return true;
        }

        if (element.NamespaceUri.StartsWith(
                "http://schemas.openxmlformats.org/drawingml/2006/diagram",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsGraphicDataForFigure(element);
    }

    private static bool IsGraphicDataForFigure(OpenXmlElement element)
    {
        if (!IsElement(element, "http://schemas.openxmlformats.org/drawingml/2006/main", "graphicData"))
            return false;

        var uri = element.GetAttributes()
            .FirstOrDefault(attribute => string.Equals(attribute.LocalName, "uri", StringComparison.OrdinalIgnoreCase))
            .Value;

        if (string.IsNullOrWhiteSpace(uri))
            return false;

        return uri.Contains("/picture", StringComparison.OrdinalIgnoreCase)
            || uri.Contains("/chart", StringComparison.OrdinalIgnoreCase)
            || uri.Contains("/diagram", StringComparison.OrdinalIgnoreCase)
            || uri.Contains("/smartart", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<OpenXmlElement> DescendantsAndSelf(OpenXmlElement element)
    {
        yield return element;

        foreach (var child in element.ChildElements)
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool IsElement(OpenXmlElement element, string namespaceUri, string localName)
    {
        return string.Equals(element.NamespaceUri, namespaceUri, StringComparison.OrdinalIgnoreCase)
            && string.Equals(element.LocalName, localName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInsideTextBoxContent(OpenXmlElement element)
    {
        OpenXmlElement? current = element;
        while (current is not null)
        {
            if (IsElement(current, "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "txbxContent")
                || IsElement(current, "urn:schemas-microsoft-com:vml", "textbox")
                || IsElement(current, "http://schemas.openxmlformats.org/drawingml/2006/main", "txBody"))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}
