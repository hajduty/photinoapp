namespace JobTracker.Application.Features.Notification;

public enum NotificationType
{
    None = 0,
    MatchingJob = 1,
    JobsAdded = 2
}

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.None;
}