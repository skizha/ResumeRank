using System.Text.Json;
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
    public Dictionary<int, List<SuitableRole>> ResumeSuitableRoles { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        // Parse suitable roles from each resume's ParsedData
        foreach (var resume in Resumes)
        {
            ResumeSuitableRoles[resume.Id] = ParseSuitableRoles(resume.ParsedData);
        }

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

    private List<SuitableRole> ParseSuitableRoles(string? parsedDataJson)
    {
        if (string.IsNullOrEmpty(parsedDataJson))
            return new List<SuitableRole>();

        try
        {
            using var doc = JsonDocument.Parse(parsedDataJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("SuitableRoles", out var rolesElement) &&
                !root.TryGetProperty("suitable_roles", out rolesElement))
            {
                return new List<SuitableRole>();
            }

            var roles = new List<SuitableRole>();
            foreach (var item in rolesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    // New format: { "role": "...", "score": N } or { "Role": "...", "Score": N }
                    var role = item.TryGetProperty("role", out var r) ? r.GetString()
                             : item.TryGetProperty("Role", out r) ? r.GetString()
                             : "Unknown";
                    var score = item.TryGetProperty("score", out var s) ? s.GetInt32()
                              : item.TryGetProperty("Score", out s) ? s.GetInt32()
                              : 0;
                    roles.Add(new SuitableRole { Role = role ?? "Unknown", Score = score });
                }
                else if (item.ValueKind == JsonValueKind.String)
                {
                    // Old format: just a string
                    roles.Add(new SuitableRole { Role = item.GetString() ?? "Unknown", Score = 0 });
                }
            }

            return roles.OrderByDescending(r => r.Score).ToList();
        }
        catch
        {
            return new List<SuitableRole>();
        }
    }
}
