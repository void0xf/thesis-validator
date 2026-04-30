namespace backend.DocumentProcessing.Lists;

public sealed class ListGroup
{
    public ListGroup(int numberingId)
    {
        NumberingId = numberingId;
    }

    public int NumberingId { get; }

    public List<ListItem> Items { get; } = [];
}
