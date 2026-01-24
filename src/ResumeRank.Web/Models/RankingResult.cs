namespace ResumeRank.Web.Models;

public class RankingResult
{
    public int Id { get; set; }
    public int ResumeId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public double OverallScore { get; set; }
    public double SkillMatchScore { get; set; }
    public double ExperienceMatchScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime RankedAt { get; set; } = DateTime.UtcNow;

    public Resume Resume { get; set; } = null!;
}
