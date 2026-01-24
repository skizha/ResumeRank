namespace ResumeRank.Web.Models;

public class Resume
{
    public int Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string? ParsedData { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
