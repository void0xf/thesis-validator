namespace backend.Application.Validation;

public sealed record AvailableValidationRule(
    string Id,
    string DisplayName,
    string Description,
    string Category,
    string DefaultSeverity);
