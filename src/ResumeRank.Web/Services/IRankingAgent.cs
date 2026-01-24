using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

public interface IRankingAgent
{
    Task<List<RankingResult>> RankAsync(List<Resume> resumes, JobDescription job);
}
