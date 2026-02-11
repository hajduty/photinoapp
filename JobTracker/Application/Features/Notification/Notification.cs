using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Notification;

[ExportTsEnum]
public enum NotificationType
{
    None = 0,
    MatchingJob = 1,
    JobsAdded = 2
}

[ExportTsInterface]
public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.None;
    public bool IsRead { get; set; } = false;
}