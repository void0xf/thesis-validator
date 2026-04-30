using DocumentFormat.OpenXml.Packaging;

namespace ThesisValidator.Rules;

public sealed class RuleContext
{
    public required WordprocessingDocument RawDocument { get; init; }
    public required DocumentContent Content { get; init; }
}
