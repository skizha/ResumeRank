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
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UploadModel> _logger;

    public UploadModel(
        IJobService jobService,
        AppDbContext db,
        IResumeParserAgent parserAgent,
        IFileStorage fileStorage,
        ILogger<UploadModel> logger)
    {
        _jobService = jobService;
        _db = db;
        _parserAgent = parserAgent;
        _fileStorage = fileStorage;
        _logger = logger;
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

        foreach (var file in Files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".docx")
                continue;

            // Determine content type
            var contentType = ext == ".pdf" ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // Upload file using the file storage service (local or S3)
            string filePath;
            using (var stream = file.OpenReadStream())
            {
                filePath = await _fileStorage.UploadAsync(id, file.FileName, stream, contentType);
            }

            _logger.LogInformation("Uploaded file {FileName} to {FilePath}", file.FileName, filePath);

            // Parse the resume using the parser agent
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
