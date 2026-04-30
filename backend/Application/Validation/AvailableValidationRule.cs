namespace backend.Application.Validation;

public sealed record AvailableValidationRule(
    string Id,
    string DisplayName,
    string Category,
    string DefaultSeverity,
    bool Enabled = true,
    bool Selectable = true);
