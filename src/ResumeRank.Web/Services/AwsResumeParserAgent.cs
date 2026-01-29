using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

/// <summary>
/// Resume parser agent that calls AWS API Gateway endpoint.
/// </summary>
public class AwsResumeParserAgent : IResumeParserAgent
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AwsResumeParserAgent> _logger;

    public AwsResumeParserAgent(HttpClient httpClient, ILogger<AwsResumeParserAgent> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ParsedResumeData> ParseAsync(string filePath)
    {
        // For AWS mode, filePath should be an S3 key or S3 URI
        var request = new { file_path = filePath };

        try
        {
            _logger.LogInformation("Calling AWS resume parser for {FilePath}", filePath);

            var response = await _httpClient.PostAsJsonAsync("parse", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AwsParserResponse>();
            if (result == null)
                throw new InvalidOperationException("AWS parser returned null response");

            _logger.LogInformation("Successfully parsed resume: {CandidateName}", result.CandidateName);

            return new ParsedResumeData
            {
                CandidateName = result.CandidateName,
                Skills = result.Skills,
                ExperienceLevel = result.ExperienceLevel,
                Summary = result.Summary,
                SuitableRoles = result.SuitableRoles
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call AWS resume parser for {FilePath}", filePath);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling AWS resume parser for {FilePath}", filePath);
            throw;
        }
    }

    private class AwsParserResponse
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
        public List<string> SuitableRoles { get; set; } = new();
    }
}
