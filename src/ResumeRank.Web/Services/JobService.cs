using System.Text.Json;
using ResumeRank.Web.Data;
using ResumeRank.Web.Models;

namespace ResumeRank.Web.Services;

public class JobService : IJobService
{
    private readonly List<JobDescription> _jobs;
    private readonly AppDbContext _db;

    public JobService(IWebHostEnvironment env, AppDbContext db)
    {
        _db = db;
        var jsonPath = Path.Combine(env.ContentRootPath, "Data", "jobs.json");
        var json = File.ReadAllText(jsonPath);
        _jobs = JsonSerializer.Deserialize<List<JobDescription>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new();
    }

    public List<JobDescription> GetAll() => _jobs;

    public JobDescription? GetById(string id) =>
        _jobs.FirstOrDefault(j => j.Id == id);

    public int GetResumeCount(string jobId) =>
        _db.Resumes.Count(r => r.JobId == jobId);
}
