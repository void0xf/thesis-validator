using Microsoft.AspNetCore.Http;

namespace backend.Endpoints.Documents;

internal sealed record DocumentUploadRequest(
    IFormFile File,
    string FileName,
    IReadOnlyList<string> SelectedRules);
