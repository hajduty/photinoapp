using JobTracker.Application.Features.JobTracker;

namespace JobTracker.Application.Infrastructure.Discord;

public interface IDiscordWebhookService
{
    Task SendNotificationAsync(string title, string description, NotificationType type = NotificationType.Info);
    Task SendJobAlertAsync(string keyword, int jobCount, List<JobInfo> jobInfos);
    Task SendHighMatchAlertAsync(string keyword, int jobCount, List<JobInfo> jobInfos);
    Task<bool> TestWebhookAsync(string webhookUrl);
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
