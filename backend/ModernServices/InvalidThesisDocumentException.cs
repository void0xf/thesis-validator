namespace backend.ModernServices;

public sealed class InvalidThesisDocumentException : Exception
{
    public InvalidThesisDocumentException(string message)
        : base(message)
    {
    }

    public InvalidThesisDocumentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
