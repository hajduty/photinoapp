namespace JobTracker.Application.Features.JobAlerts;

public class JobAlert
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int CheckIntervalHours { get; set; } = 1;
    public int RadiusKm { get; set; } = 100;
}