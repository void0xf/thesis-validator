using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services;

/// <summary>
/// Service for adding comments to Word documents.
/// </summary>
public class DocumentCommentService
{
    private int _commentIdCounter = 0;
    private readonly Dictionary<int, (string Author, string Text)> _pendingComments = new();

    /// <summary>
    /// Add a comment to a specific run in the document.
    /// </summary>
    public void AddCommentToRun(WordprocessingDocument doc, Run run, string commentText, string author = "Thesis Validator")
    {
        var commentId = _commentIdCounter++;

        var commentsPart = doc.MainDocumentPart!.WordprocessingCommentsPart;
        if (commentsPart == null)
        {
            commentsPart = doc.MainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments();
        }

        var comment = new Comment
        {
            Id = commentId.ToString(),
            Author = author,
            Date = DateTime.Now,
            Initials = GetInitials(author)
        };

        comment.AppendChild(new Paragraph(
            new Run(new Text(commentText))
        ));

        commentsPart.Comments.AppendChild(comment);

        var parent = run.Parent;
        if (parent == null) return;

        var commentRangeStart = new CommentRangeStart { Id = commentId.ToString() };
        var commentRangeEnd = new CommentRangeEnd { Id = commentId.ToString() };
        var commentReference = new Run(new CommentReference { Id = commentId.ToString() });

        parent.InsertBefore(commentRangeStart, run);
        parent.InsertAfter(commentRangeEnd, run);
        parent.InsertAfter(commentReference, commentRangeEnd);
    }

    /// <summary>
    /// Add a comment to an entire paragraph.
    /// </summary>
    public void AddCommentToParagraph(WordprocessingDocument doc, Paragraph paragraph, string commentText, string author = "Thesis Validator")
    {
        var firstRun = paragraph.Elements<Run>().FirstOrDefault();
        if (firstRun != null)
        {
            AddCommentToRun(doc, firstRun, commentText, author);
            return;
        }

        var commentId = _commentIdCounter++;

        var commentsPart = doc.MainDocumentPart!.WordprocessingCommentsPart;
        if (commentsPart == null)
        {
            commentsPart = doc.MainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments();
        }

        var comment = new Comment
        {
            Id = commentId.ToString(),
            Author = author,
            Date = DateTime.Now,
            Initials = GetInitials(author)
        };

        comment.AppendChild(new Paragraph(
            new Run(new Text(commentText))
        ));

        commentsPart.Comments.AppendChild(comment);

        var commentRangeStart = new CommentRangeStart { Id = commentId.ToString() };
        var commentRangeEnd = new CommentRangeEnd { Id = commentId.ToString() };
        var commentReference = new Run(new CommentReference { Id = commentId.ToString() });

        paragraph.InsertAt(commentRangeStart, 0);
        paragraph.AppendChild(commentRangeEnd);
        paragraph.AppendChild(commentReference);
    }

    /// <summary>
    /// Add a comment at a specific character offset within a paragraph.
    /// </summary>
    public void AddCommentAtOffset(WordprocessingDocument doc, Paragraph paragraph, int offset, int length, string commentText, string author = "Thesis Validator")
    {
        var runs = paragraph.Elements<Run>().ToList();
        var currentOffset = 0;

        foreach (var run in runs)
        {
            var runText = GetRunText(run);
            var runStart = currentOffset;
            var runEnd = currentOffset + runText.Length;

            if (offset < runEnd && offset + length > runStart)
            {
                AddCommentToRun(doc, run, commentText, author);
                return;
            }

            currentOffset = runEnd;
        }

        if (runs.Count > 0)
        {
            AddCommentToRun(doc, runs[0], commentText, author);
        }
        else
        {
            AddCommentToParagraph(doc, paragraph, commentText, author);
        }
    }

    /// <summary>
    /// Save the document with comments to a new stream.
    /// </summary>
    public static MemoryStream SaveDocumentWithComments(WordprocessingDocument doc)
    {
        var outputStream = new MemoryStream();

        using (var clone = doc.Clone(outputStream))
        {
        }

        outputStream.Position = 0;
        return outputStream;
    }

    private static string GetRunText(Run run)
    {
        return string.Concat(run.Elements<Text>().Select(t => t.Text));
    }

    private static string GetInitials(string author)
    {
        var words = author.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpper(w[0])));
    }
}
