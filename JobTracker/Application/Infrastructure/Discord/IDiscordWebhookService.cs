namespace JobTracker.Application.Infrastructure.Discord;

public interface IDiscordWebhookService
{
    Task SendNotificationAsync(string title, string description, NotificationType type = NotificationType.Info);
    Task SendJobAlertAsync(string keyword, int jobCount, string[]? jobTitles = null);
    Task<bool> TestWebhookAsync(string webhookUrl);
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
