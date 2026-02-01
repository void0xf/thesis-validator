using backend.Models;
using backend.Rules;
using backend.Services;
using backend.Tests.Helpers;
using Backend.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace backend.Tests.Rules;

public class GrammarRuleTests
{
    private static UniversityConfig CreateConfig(bool checkGrammar = true, string language = "en-US")
    {
        return new UniversityConfig
        {
            CheckGrammar = checkGrammar,
            Language = language
        };
    }

    private static LanguageToolService CreateMockLanguageToolService(
        LanguageToolResponse? response = null,
        bool isAvailable = true)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        // Mock languages endpoint (availability check)
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/v2/languages")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = isAvailable ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("[]")
            });

        // Mock check endpoint
        var checkResponse = response ?? new LanguageToolResponse { Matches = new List<LanguageToolMatch>() };
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/v2/check")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(checkResponse))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c.GetValue<string>("LanguageTool:BaseUrl", It.IsAny<string>()))
            .Returns("http://localhost:8010");

        return new LanguageToolService(httpClient, configMock.Object);
    }

    [Fact]
    public void Validate_GrammarCheckDisabled_ReturnsNoErrors()
    {
        // Arrange
        var service = CreateMockLanguageToolService();
        var rule = new GrammarRule(service);
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("This sentence has an error.", "Times New Roman")
        );
        var config = CreateConfig(checkGrammar: false);

        // Act
        var errors = rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NoGrammarErrors_ReturnsEmpty()
    {
        // Arrange
        var service = CreateMockLanguageToolService(new LanguageToolResponse
        {
            Matches = new List<LanguageToolMatch>()
        });
        var rule = new GrammarRule(service);
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("This sentence is correct.", "Times New Roman")
        );
        var config = CreateConfig();

        // Act
        var errors = rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithSpellingError_ReturnsError()
    {
        // Arrange
        var service = CreateMockLanguageToolService(new LanguageToolResponse
        {
            Matches = new List<LanguageToolMatch>
            {
                new LanguageToolMatch
                {
                    Message = "Possible spelling mistake found",
                    Offset = 5,
                    Length = 5,
                    Sentence = "This speling is wrong.",
                    Replacements = new List<LanguageToolReplacement>
                    {
                        new() { Value = "spelling" }
                    },
                    Rule = new LanguageToolRule
                    {
                        Id = "MORFOLOGIK_RULE_EN_US",
                        IssueType = "misspelling",
                        Category = new LanguageToolCategory { Id = "TYPOS", Name = "Possible Typo" }
                    }
                }
            }
        });
        var rule = new GrammarRule(service);
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("This speling is wrong.", "Times New Roman")
        );
        var config = CreateConfig();

        // Act
        var errors = rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Contains("Spelling", errors[0].Message);
        Assert.Contains("spelling", errors[0].Message); // suggestion
        Assert.True(errors[0].IsError);
    }

    [Fact]
    public void Validate_WithGrammarError_ReturnsError()
    {
        // Arrange
        var service = CreateMockLanguageToolService(new LanguageToolResponse
        {
            Matches = new List<LanguageToolMatch>
            {
                new LanguageToolMatch
                {
                    Message = "The verb 'are' does not agree with the subject 'He'",
                    Offset = 3,
                    Length = 3,
                    Sentence = "He are going home.",
                    Replacements = new List<LanguageToolReplacement>
                    {
                        new() { Value = "is" }
                    },
                    Rule = new LanguageToolRule
                    {
                        Id = "SUBJECT_VERB_AGREEMENT",
                        IssueType = "grammar",
                        Category = new LanguageToolCategory { Id = "GRAMMAR", Name = "Grammar" }
                    }
                }
            }
        });
        var rule = new GrammarRule(service);
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("He are going home.", "Times New Roman")
        );
        var config = CreateConfig();

        // Act
        var errors = rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Contains("Grammar", errors[0].Message);
        Assert.True(errors[0].IsError);
    }

    [Fact]
    public void Validate_ServiceUnavailable_ReturnsWarning()
    {
        // Arrange
        var service = CreateMockLanguageToolService(isAvailable: false);
        var rule = new GrammarRule(service);
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Some text here.", "Times New Roman")
        );
        var config = CreateConfig();

        // Act
        var errors = rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Contains("not available", errors[0].Message);
        Assert.False(errors[0].IsError); // Warning, not error
    }

    [Fact]
    public void Validate_RuleNameIsCorrect()
    {
        // Arrange
        var service = CreateMockLanguageToolService();
        var rule = new GrammarRule(service);

        // Assert
        Assert.Equal("Grammar", rule.Name);
    }

    [Fact]
    public void Validate_LocationIncludesPageAndLine()
    {
        // Arrange
        var service = CreateMockLanguageToolService(new LanguageToolResponse
        {
            Matches = new List<LanguageToolMatch>
            {
                new LanguageToolMatch
                {
                    Message = "Spelling mistake",
                    Offset = 0,
                    Length = 4,
                    Rule = new LanguageToolRule { IssueType = "misspelling" }
                }
            }
        });
        var rule = new GrammarRule(service);
        using var docx = DocxTestHelper.CreateInMemoryDocx(
            ("Tset text.", "Times New Roman")
        );
        var config = CreateConfig();

        // Act
        var errors = rule.Validate(docx.Document, config).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Equal(1, errors[0].Location.PageNumber);
        Assert.True(errors[0].Location.LineNumber >= 1);
        Assert.Equal(1, errors[0].Location.Paragraph);
    }
}
