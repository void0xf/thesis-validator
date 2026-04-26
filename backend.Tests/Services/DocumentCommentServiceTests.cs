using backend.Services;
using backend.Tests.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Tests.Services;

public class DocumentCommentServiceTests
{
    [Fact]
    public void AddCommentToRun_WhenDocumentHasExistingComments_UsesNextAvailableId()
    {
        using var docx = CreateDocxWithExistingComment("7");
        var doc = docx.Document;
        var run = doc.MainDocumentPart!.Document.Body!.Descendants<Run>().First();

        new DocumentCommentService().AddCommentToRun(doc, run, "New validation comment");

        var commentIds = doc.MainDocumentPart.WordprocessingCommentsPart!.Comments!
            .Elements<Comment>()
            .Select(comment => comment.Id!.Value)
            .ToList();

        Assert.Contains("7", commentIds);
        Assert.Contains("8", commentIds);
        Assert.Equal(2, commentIds.Count);
    }

    private static InMemoryDocx CreateDocxWithExistingComment(string commentId)
    {
        var stream = new MemoryStream();
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            new Paragraph(new Run(new Text("Paragraph with existing comments")))));

        var commentsPart = mainPart.AddNewPart<WordprocessingCommentsPart>();
        var existingComment = new Comment
        {
            Id = commentId,
            Author = "Existing Author",
            Initials = "EA"
        };
        existingComment.AppendChild(new Paragraph(new Run(new Text("Existing comment"))));
        commentsPart.Comments = new Comments(existingComment);

        mainPart.Document.Save();
        return new InMemoryDocx(doc, stream);
    }
}
