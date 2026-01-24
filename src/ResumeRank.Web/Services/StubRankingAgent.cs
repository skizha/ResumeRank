using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

public class StubRankingAgent : IRankingAgent
{
    public Task<List<RankingResult>> RankAsync(List<Resume> resumes, JobDescription job)
    {
        var random = new Random();
        var results = resumes.Select(resume => new RankingResult
        {
            ResumeId = resume.Id,
            JobId = job.Id,
            SkillMatchScore = Math.Round(random.NextDouble() * 100, 1),
            ExperienceMatchScore = Math.Round(random.NextDouble() * 100, 1),
            OverallScore = 0,
            Summary = $"Stub ranking for {resume.CandidateName}",
            RankedAt = DateTime.UtcNow
        }).ToList();

        foreach (var result in results)
        {
            result.OverallScore = Math.Round((result.SkillMatchScore * 0.6 + result.ExperienceMatchScore * 0.4), 1);
        }

        return Task.FromResult(results);
    }
}
