namespace ResumeRank.Web.Models;

public class ParsedResumeData
{
    public string CandidateName { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string? ExperienceLevel { get; set; }
    public string? Summary { get; set; }
}
