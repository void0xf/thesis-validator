using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.ModernServices.CodeBlocks;

public interface ICodeBlockDetector
{
    CodeBlockDetectionResult Analyze(Paragraph paragraph, MainDocumentPart mainPart);

    bool IsCodeBlock(Paragraph paragraph, MainDocumentPart mainPart);
}
