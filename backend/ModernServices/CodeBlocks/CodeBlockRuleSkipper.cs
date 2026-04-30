using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.ModernServices.CodeBlocks;

public static class CodeBlockRuleSkipper
{
    public static bool ShouldSkip(WordprocessingDocument doc, Paragraph paragraph, ICodeBlockDetector detector)
    {
        var mainPart = doc.MainDocumentPart;
        return mainPart is not null && detector.IsCodeBlock(paragraph, mainPart);
    }
}
