using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ResumeRank.Web.Data;
using ResumeRank.Web.Models;
using ResumeRank.Web.Services;

namespace ResumeRank.Web.Pages.Jobs;

public class DetailModel : PageModel
{
    private readonly IJobService _jobService;
    private readonly AppDbContext _db;
    private readonly IRankingAgent _rankingAgent;

    public DetailModel(IJobService jobService, AppDbContext db, IRankingAgent rankingAgent)
    {
        _jobService = jobService;
        _db = db;
        _rankingAgent = rankingAgent;
    }

    public JobDescription Job { get; set; } = null!;
    public List<Resume> Resumes { get; set; } = new();
    public List<RankingResult> Rankings { get; set; } = new();

    public IActionResult OnGet(string id)
    {
        var job = _jobService.GetById(id);
        if (job == null)
            return NotFound();

        Job = job;
        Resumes = _db.Resumes.Where(r => r.JobId == id).OrderByDescending(r => r.UploadedAt).ToList();
        Rankings = _db.RankingResults
            .Include(r => r.Resume)
            .Where(r => r.JobId == id)
            .OrderByDescending(r => r.OverallScore)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostRankAsync(string id)
    {
        var job = _jobService.GetById(id);
        if (job == null)
            return NotFound();

        var resumes = _db.Resumes.Where(r => r.JobId == id).ToList();
        if (resumes.Count == 0)
            return RedirectToPage(new { id });

        // Remove old rankings for this job
        var oldRankings = _db.RankingResults.Where(r => r.JobId == id);
        _db.RankingResults.RemoveRange(oldRankings);

        var results = await _rankingAgent.RankAsync(resumes, job);
        _db.RankingResults.AddRange(results);
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, int resumeId)
    {
        var resume = await _db.Resumes.FindAsync(resumeId);
        if (resume != null)
        {
            // Delete associated rankings
            var rankings = _db.RankingResults.Where(r => r.ResumeId == resumeId);
            _db.RankingResults.RemoveRange(rankings);

            // Delete file
            if (System.IO.File.Exists(resume.FilePath))
                System.IO.File.Delete(resume.FilePath);

            _db.Resumes.Remove(resume);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }
}
