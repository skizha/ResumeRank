using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

public interface IJobService
{
    List<JobDescription> GetAll();
    JobDescription? GetById(string id);
    int GetResumeCount(string jobId);
}
