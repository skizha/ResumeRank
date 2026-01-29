using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

/// <summary>
/// Ranking agent that calls AWS API Gateway endpoint.
/// </summary>
public class AwsRankingAgent : IRankingAgent
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AwsRankingAgent> _logger;

    public AwsRankingAgent(HttpClient httpClient, ILogger<AwsRankingAgent> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<RankingResult>> RankAsync(List<Resume> resumes, JobDescription job)
    {
        var request = new AwsRankRequest
        {
            Resumes = resumes.Select(r =>
            {
                var parsedData = !string.IsNullOrEmpty(r.ParsedData)
                    ? JsonSerializer.Deserialize<ParsedResumeData>(r.ParsedData,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null;

                return new AwsRankResume
                {
                    ResumeId = r.Id,
                    CandidateName = r.CandidateName,
                    Skills = parsedData?.Skills ?? new List<string>(),
                    ExperienceLevel = parsedData?.ExperienceLevel,
                    Summary = parsedData?.Summary
                };
            }).ToList(),
            Job = new AwsRankJob
            {
                JobId = job.Id,
                Title = job.Title,
                Description = job.Description,
                RequiredSkills = job.RequiredSkills,
                PreferredSkills = job.PreferredSkills,
                ExperienceLevel = job.ExperienceLevel
            }
        };

        try
        {
            _logger.LogInformation("Calling AWS ranking agent for job {JobTitle} with {ResumeCount} resumes",
                job.Title, resumes.Count);

            var response = await _httpClient.PostAsJsonAsync("/rank", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AwsRankResponse>();
            if (result == null)
                throw new InvalidOperationException("AWS ranking agent returned null response");

            _logger.LogInformation("Successfully ranked {Count} resumes", result.Rankings.Count);

            return result.Rankings.Select(r => new RankingResult
            {
                ResumeId = r.ResumeId,
                JobId = job.Id,
                SkillMatchScore = r.SkillMatchScore,
                ExperienceMatchScore = r.ExperienceMatchScore,
                OverallScore = r.OverallScore,
                Summary = r.Summary,
                RankedAt = DateTime.UtcNow
            }).ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call AWS ranking agent");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling AWS ranking agent");
            throw;
        }
    }

    private class AwsRankRequest
    {
        [JsonPropertyName("resumes")]
        public List<AwsRankResume> Resumes { get; set; } = new();

        [JsonPropertyName("job")]
        public AwsRankJob Job { get; set; } = null!;
    }

    private class AwsRankResume
    {
        [JsonPropertyName("resume_id")]
        public int ResumeId { get; set; }

        [JsonPropertyName("candidate_name")]
        public string CandidateName { get; set; } = string.Empty;

        [JsonPropertyName("skills")]
        public List<string> Skills { get; set; } = new();

        [JsonPropertyName("experience_level")]
        public string? ExperienceLevel { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }
    }

    private class AwsRankJob
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("required_skills")]
        public List<string> RequiredSkills { get; set; } = new();

        [JsonPropertyName("preferred_skills")]
        public List<string> PreferredSkills { get; set; } = new();

        [JsonPropertyName("experience_level")]
        public string ExperienceLevel { get; set; } = string.Empty;
    }

    private class AwsRankResponse
    {
        [JsonPropertyName("rankings")]
        public List<AwsRankScore> Rankings { get; set; } = new();
    }

    private class AwsRankScore
    {
        [JsonPropertyName("resume_id")]
        public int ResumeId { get; set; }

        [JsonPropertyName("skill_match_score")]
        public double SkillMatchScore { get; set; }

        [JsonPropertyName("experience_match_score")]
        public double ExperienceMatchScore { get; set; }

        [JsonPropertyName("overall_score")]
        public double OverallScore { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
    }
}
