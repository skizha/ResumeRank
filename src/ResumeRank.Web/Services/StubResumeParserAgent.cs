using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

public class StubResumeParserAgent : IResumeParserAgent
{
    public Task<ParsedResumeData> ParseAsync(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var candidateName = fileName
            .Replace("_", " ")
            .Replace("-", " ");

        var result = new ParsedResumeData
        {
            CandidateName = candidateName,
            Skills = new List<string>(),
            ExperienceLevel = "Unknown",
            Summary = "Parsed from uploaded resume file."
        };

        return Task.FromResult(result);
    }
}
