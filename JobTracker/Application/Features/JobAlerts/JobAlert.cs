using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobAlerts;

[Index(nameof(Keyword), IsUnique = true)]
public class JobAlert
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public int CheckIntervalHours { get; set; } = 1;
}