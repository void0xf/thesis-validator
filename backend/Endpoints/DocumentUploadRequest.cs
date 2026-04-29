using Microsoft.AspNetCore.Http;

namespace backend.Endpoints;

internal sealed record DocumentUploadRequest(
    IFormFile File,
    string FileName,
    IReadOnlyList<string> SelectedRules);
