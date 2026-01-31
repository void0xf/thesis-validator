using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace backend.Services;

/// <summary>
/// Service for communicating with the LanguageTool API for grammar and spell checking.
/// </summary>
public class LanguageToolService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public LanguageToolService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration.GetValue<string>("LanguageTool:BaseUrl") ?? "http://localhost:8010";
    }

    /// <summary>
    /// Check text for grammar and spelling errors.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="language">Language code (e.g., "en-US", "pl-PL").</param>
    /// <returns>List of grammar/spelling matches found.</returns>
    public async Task<LanguageToolResponse> CheckTextAsync(string text, string language = "pl-PL")
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["text"] = text,
            ["language"] = language,
            ["enabledOnly"] = "false"
        });

        var response = await _httpClient.PostAsync($"{_baseUrl}/v2/check", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LanguageToolResponse>();
        return result ?? new LanguageToolResponse();
    }

    /// <summary>
    /// Check if the LanguageTool service is available.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/v2/languages");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

#region LanguageTool API Response Models

public class LanguageToolResponse
{
    [JsonPropertyName("software")]
    public LanguageToolSoftware? Software { get; set; }

    [JsonPropertyName("language")]
    public LanguageToolLanguage? Language { get; set; }

    [JsonPropertyName("matches")]
    public List<LanguageToolMatch> Matches { get; set; } = new();
}

public class LanguageToolSoftware
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

public class LanguageToolLanguage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("detectedLanguage")]
    public DetectedLanguage? DetectedLanguage { get; set; }
}

public class DetectedLanguage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public class LanguageToolMatch
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("shortMessage")]
    public string ShortMessage { get; set; } = string.Empty;

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("replacements")]
    public List<LanguageToolReplacement> Replacements { get; set; } = new();

    [JsonPropertyName("context")]
    public LanguageToolContext? Context { get; set; }

    [JsonPropertyName("sentence")]
    public string Sentence { get; set; } = string.Empty;

    [JsonPropertyName("rule")]
    public LanguageToolRule? Rule { get; set; }
}

public class LanguageToolReplacement
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class LanguageToolContext
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }
}

public class LanguageToolRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("issueType")]
    public string IssueType { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public LanguageToolCategory? Category { get; set; }
}

public class LanguageToolCategory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

#endregion
