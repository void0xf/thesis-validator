namespace backend.Services;

public sealed class InvalidThesisDocumentException : Exception
{
    public InvalidThesisDocumentException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
