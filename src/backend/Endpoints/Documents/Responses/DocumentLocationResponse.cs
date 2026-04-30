using ThesisValidator.Rules;

namespace backend.Endpoints.Documents.Responses;

public sealed class DocumentLocationResponse
{
    public int Paragraph { get; set; }

    public int Run { get; set; }

    public int CharacterOffset { get; set; }

    public int Length { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Section { get; set; } = string.Empty;

    public static DocumentLocationResponse From(DocumentLocation location)
    {
        return new DocumentLocationResponse
        {
            Paragraph = location.Paragraph,
            Run = location.Run,
            CharacterOffset = location.CharacterOffset,
            Length = location.Length,
            Text = location.Text,
            Section = location.Section
        };
    }
}
