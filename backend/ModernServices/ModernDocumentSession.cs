using DocumentFormat.OpenXml.Packaging;

namespace backend.ModernServices;

public sealed class ModernDocumentSession
{
    public WordprocessingDocument OpenRead(Stream stream)
    {
        return OpenDocument(stream, isEditable: false);
    }

    public ModernEditableDocument OpenEditableCopy(Stream stream)
    {
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var document = OpenDocument(memoryStream, isEditable: true);
        return new ModernEditableDocument(document, memoryStream);
    }

    public MemoryStream SaveAnnotated(WordprocessingDocument document)
    {
        try
        {
            document.MainDocumentPart?.Document.Save();
            return ModernDocumentCommentService.SaveDocumentWithComments(document);
        }
        catch (Exception ex) when (IsDocumentProcessingException(ex))
        {
            throw new InvalidThesisDocumentException(
                "The uploaded file could not be saved as an annotated DOCX document.",
                ex);
        }
    }

    private static WordprocessingDocument OpenDocument(Stream stream, bool isEditable)
    {
        try
        {
            var document = WordprocessingDocument.Open(stream, isEditable);
            if (document.MainDocumentPart?.Document is not null)
            {
                return document;
            }

            document.Dispose();
            throw new InvalidThesisDocumentException(
                "The uploaded file is not a valid Wordprocessing document.");
        }
        catch (InvalidThesisDocumentException)
        {
            throw;
        }
        catch (Exception ex) when (IsDocumentProcessingException(ex))
        {
            throw new InvalidThesisDocumentException(
                "The uploaded file could not be opened as a DOCX document.",
                ex);
        }
    }

    private static bool IsDocumentProcessingException(Exception exception)
    {
        return exception is OpenXmlPackageException
            or FileFormatException
            or InvalidDataException
            or IOException
            or NotSupportedException
            or InvalidOperationException
            or ArgumentException
            or ObjectDisposedException;
    }
}

public sealed class ModernEditableDocument : IDisposable
{
    private readonly MemoryStream _stream;

    public ModernEditableDocument(
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
