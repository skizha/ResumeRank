using Microsoft.AspNetCore.Mvc.RazorPages;
using ResumeRank.Web.Models;
using ResumeRank.Web.Services;

namespace ResumeRank.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IJobService _jobService;

    public IndexModel(IJobService jobService)
    {
        _jobService = jobService;
    }

    public List<JobDescription> Jobs { get; set; } = new();
    public Dictionary<string, int> ResumeCounts { get; set; } = new();

    public void OnGet()
    {
        Jobs = _jobService.GetAll();
        foreach (var job in Jobs)
        {
            ResumeCounts[job.Id] = _jobService.GetResumeCount(job.Id);
        }
    }
}
