using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

public interface IResumeParserAgent
{
    Task<ParsedResumeData> ParseAsync(string filePath);
}
