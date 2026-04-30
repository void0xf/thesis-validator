using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.ModernServices;

public sealed class ModernDocumentCommentService
{
    private int _commentIdCounter;

    public void AddCommentToRun(
        WordprocessingDocument document,
        Run run,
        string commentText,
        string author = "Thesis Validator")
    {
        var commentsPart = GetOrCreateCommentsPart(document);
        var commentId = GetNextCommentId(commentsPart);
        var commentIdText = commentId.ToString();

        commentsPart.Comments!.AppendChild(CreateComment(commentIdText, commentText, author));

        var parent = run.Parent;
        if (parent is null)
            return;

        var commentRangeStart = new CommentRangeStart { Id = commentIdText };
        var commentRangeEnd = new CommentRangeEnd { Id = commentIdText };
        var commentReference = new Run(new CommentReference { Id = commentIdText });

        parent.InsertBefore(commentRangeStart, run);
        parent.InsertAfter(commentRangeEnd, run);
        parent.InsertAfter(commentReference, commentRangeEnd);
    }

    public void AddCommentToParagraph(
        WordprocessingDocument document,
        Paragraph paragraph,
        string commentText,
        string author = "Thesis Validator")
    {
        var firstRun = paragraph.Elements<Run>().FirstOrDefault();
        if (firstRun is not null)
        {
            AddCommentToRun(document, firstRun, commentText, author);
            return;
        }

        var commentsPart = GetOrCreateCommentsPart(document);
        var commentId = GetNextCommentId(commentsPart);
        var commentIdText = commentId.ToString();

        commentsPart.Comments!.AppendChild(CreateComment(commentIdText, commentText, author));

        paragraph.InsertAt(new CommentRangeStart { Id = commentIdText }, 0);
        paragraph.AppendChild(new CommentRangeEnd { Id = commentIdText });
        paragraph.AppendChild(new Run(new CommentReference { Id = commentIdText }));
    }

    public static MemoryStream SaveDocumentWithComments(WordprocessingDocument document)
    {
        var outputStream = new MemoryStream();
        using (document.Clone(outputStream))
        {
        }

        outputStream.Position = 0;
        return outputStream;
    }

    private static Comment CreateComment(string id, string commentText, string author)
    {
        var comment = new Comment
        {
            Id = id,
            Author = author,
            Date = DateTime.Now,
            Initials = GetInitials(author)
        };

        comment.AppendChild(new Paragraph(new Run(new Text(commentText))));
        return comment;
    }

    private static WordprocessingCommentsPart GetOrCreateCommentsPart(WordprocessingDocument document)
    {
        var commentsPart = document.MainDocumentPart!.WordprocessingCommentsPart
            ?? document.MainDocumentPart.AddNewPart<WordprocessingCommentsPart>();

        commentsPart.Comments ??= new Comments();
        return commentsPart;
    }

    private int GetNextCommentId(WordprocessingCommentsPart commentsPart)
    {
        var maxExistingId = commentsPart.Comments?
            .Elements<Comment>()
            .Select(comment => int.TryParse(comment.Id?.Value, out var id) ? id : -1)
            .DefaultIfEmpty(-1)
            .Max() ?? -1;

        _commentIdCounter = Math.Max(_commentIdCounter, maxExistingId + 1);
        return _commentIdCounter++;
    }

    private static string GetInitials(string author)
    {
        return string.Concat(
            author
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0])));
    }
}
