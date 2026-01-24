namespace ResumeRank.Web.Models;

public class JobDescription
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public List<string> PreferredSkills { get; set; } = new();
    public string ExperienceLevel { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
