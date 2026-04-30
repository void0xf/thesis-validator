using System.Text.Json.Serialization;

namespace backend.Infrastructure.LanguageTool;

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
