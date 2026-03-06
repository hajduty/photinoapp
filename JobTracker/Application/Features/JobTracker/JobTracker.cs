using JobTracker.Application.Features.Tags;

namespace JobTracker.Application.Features.JobTracker;

public class JobTracker
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Source { get; set; } = "All";
    public string Location { get; set; } = "All";
    public bool IsActive { get; set; } = true;
    public int CheckIntervalHours { get; set; } = 1;
    public virtual List<Tag> Tags { get; set; } = new List<Tag>();
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
}