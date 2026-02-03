namespace JobTracker.Application.Features.Postings;

public class Posting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    public string Url { get; set; } = string.Empty;
    public string OriginUrl { get; set; } = string.Empty;
    public string CompanyImage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastApplicationDate { get; set; } = DateTime.UtcNow;
}