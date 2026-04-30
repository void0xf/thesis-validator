using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.DocumentProcessing.CodeBlocks;

public interface ICodeBlockDetector
{
    CodeBlockDetectionResult Analyze(Paragraph paragraph, MainDocumentPart mainPart);

    bool IsCodeBlock(Paragraph paragraph, MainDocumentPart mainPart);
}
