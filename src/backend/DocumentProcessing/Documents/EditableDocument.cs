using DocumentFormat.OpenXml.Packaging;

namespace backend.DocumentProcessing.Documents;

public sealed class EditableDocument : IDisposable
{
    private readonly MemoryStream _stream;

    public EditableDocument(
        WordprocessingDocument document,
        MemoryStream stream)
    {
        Document = document;
        _stream = stream;
    }

    public WordprocessingDocument Document { get; }

    public void Dispose()
    {
        Document.Dispose();
        _stream.Dispose();
    }
}
