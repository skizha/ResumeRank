using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

public class HttpResumeParserAgent : IResumeParserAgent
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpResumeParserAgent> _logger;

    public HttpResumeParserAgent(HttpClient httpClient, ILogger<HttpResumeParserAgent> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ParsedResumeData> ParseAsync(string filePath)
    {
        var request = new { file_path = filePath };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/parse", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ParserAgentResponse>();
            if (result == null)
                throw new InvalidOperationException("Parser returned null response");

            return new ParsedResumeData
            {
                CandidateName = result.CandidateName,
                Skills = result.Skills,
                ExperienceLevel = result.ExperienceLevel,
                Summary = result.Summary,
                SuitableRoles = result.SuitableRoles.Select(r => new SuitableRole
                {
                    Role = r.Role,
                    Score = r.Score
                }).ToList()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call resume parser agent for {FilePath}", filePath);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling resume parser agent for {FilePath}", filePath);
            throw;
        }
    }

    private class ParserAgentResponse
    {
        [JsonPropertyName("candidate_name")]
        public string CandidateName { get; set; } = string.Empty;

        [JsonPropertyName("skills")]
        public List<string> Skills { get; set; } = new();

        [JsonPropertyName("experience_level")]
        public string? ExperienceLevel { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("suitable_roles")]
        public List<SuitableRoleResponse> SuitableRoles { get; set; } = new();
    }

    private class SuitableRoleResponse
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public int Score { get; set; }
    }
}
