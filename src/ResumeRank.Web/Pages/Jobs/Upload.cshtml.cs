using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ResumeRank.Web.Data;
using ResumeRank.Web.Models;
using ResumeRank.Web.Services;

namespace ResumeRank.Web.Pages.Jobs;

public class UploadModel : PageModel
{
    private readonly IJobService _jobService;
    private readonly AppDbContext _db;
    private readonly IResumeParserAgent _parserAgent;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public UploadModel(
        IJobService jobService,
        AppDbContext db,
        IResumeParserAgent parserAgent,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        _jobService = jobService;
        _db = db;
        _parserAgent = parserAgent;
        _configuration = configuration;
        _env = env;
    }

    public JobDescription Job { get; set; } = null!;

    [BindProperty]
    public List<IFormFile> Files { get; set; } = new();

    public IActionResult OnGet(string id)
    {
        var job = _jobService.GetById(id);
        if (job == null)
            return NotFound();

        Job = job;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var job = _jobService.GetById(id);
        if (job == null)
            return NotFound();

        Job = job;

        if (Files.Count == 0)
        {
            ModelState.AddModelError("Files", "Please select at least one file.");
            return Page();
        }

        var uploadPath = Path.Combine(_env.ContentRootPath,
            _configuration["FileStorage:UploadPath"] ?? "uploads", id);
        Directory.CreateDirectory(uploadPath);

        foreach (var file in Files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".docx")
                continue;

            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadPath, safeFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var parsed = await _parserAgent.ParseAsync(filePath);

            var resume = new Resume
            {
                JobId = id,
                FileName = file.FileName,
                FilePath = filePath,
                CandidateName = parsed.CandidateName,
                ParsedData = System.Text.Json.JsonSerializer.Serialize(parsed),
                UploadedAt = DateTime.UtcNow
            };

            _db.Resumes.Add(resume);
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Jobs/Detail", new { id });
    }
}
