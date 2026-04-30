using System.Net.Http.Json;

namespace backend.Infrastructure.LanguageTool;

/// <summary>
/// Service for communicating with the LanguageTool API for grammar and spell checking.
/// </summary>
public class LanguageToolClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public LanguageToolClient(HttpClient httpClient, IConfiguration configuration)
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
