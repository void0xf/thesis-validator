using backend.Services.Comments;
using DocumentFormat.OpenXml.Packaging;
using ThesisValidator.Rules;

namespace backend.ModernServices;

public sealed class ModernAnnotationApplier
{
    public void Apply(
        WordprocessingDocument document,
        IEnumerable<ModernRuleExecution> executions)
    {
        var commentService = new DocumentCommentService();

        foreach (var execution in executions)
        {
            AddCommentForProblem(
                commentService,
                document,
                execution.Problem);
        }
    }

    private static void AddCommentForProblem(
        DocumentCommentService commentService,
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
        }
    }
}
