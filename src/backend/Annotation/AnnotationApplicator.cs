using backend.Application.Validation;
using DocumentFormat.OpenXml.Packaging;
using ThesisValidator.Rules;

namespace backend.Annotation;

public sealed class AnnotationApplicator
{
    public void Apply(
        WordprocessingDocument document,
        IEnumerable<RuleExecution> executions)
    {
        var commentService = new CommentWriter();

        foreach (var execution in executions)
        {
            AddCommentForProblem(
                commentService,
                document,
                execution.Problem);
        }
    }

    private static void AddCommentForProblem(
        CommentWriter commentService,
        WordprocessingDocument document,
        RuleProblem problem)
    {
        switch (problem.AnnotationTarget)
        {
            case ParagraphAnnotationTarget paragraphTarget:
                commentService.AddCommentToParagraph(
                    document,
                    paragraphTarget.Paragraph,
                    problem.Message);
                break;

            case RunAnnotationTarget runTarget:
                commentService.AddCommentToRun(
                    document,
                    runTarget.Run,
                    problem.Message);
                break;

            case TableAnnotationTarget tableTarget:
                commentService.AddCommentToTable(
                    document,
                    tableTarget.Table,
                    problem.Message);
                break;
        }
    }
}
