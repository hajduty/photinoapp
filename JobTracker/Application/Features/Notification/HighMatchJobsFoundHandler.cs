using JobTracker.Application.Events;
using JobTracker.Application.Features.Jobs;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Discord;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Notification;
public class HighMatchJobsFoundEventHandler : IEventHandler<HighMatchJobsFoundEvent>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IUiEventEmitter _eventEmitter;
    private readonly IDiscordWebhookService _discord;

    public HighMatchJobsFoundEventHandler(
        IDbContextFactory<AppDbContext> dbFactory,
        IUiEventEmitter eventEmitter,
        IDiscordWebhookService discord)
    {
        _dbFactory = dbFactory;
        _eventEmitter = eventEmitter;
        _discord = discord;
    }

    public async Task HandleAsync(HighMatchJobsFoundEvent domainEvent)
    {
        var title = domainEvent.JobCount == 1
            ? "1 high-match job found!"
            : $"{domainEvent.JobCount} high-match jobs found!";

        var description = domainEvent.JobCount == 1
            ? $"A job strongly matching your profile was found for '{domainEvent.Keyword}'."
            : $"{domainEvent.JobCount} jobs strongly matching your profile were found for '{domainEvent.Keyword}'.";

        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var notification = new Notification
        {
            Title = title,
            Description = description,
            Type = NotificationType.MatchingJob,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        _eventEmitter.Emit("notification.new", notification);

        var jobTitles = domainEvent.Jobs.Select(j => j.Title).ToArray();
        await _discord.SendHighMatchAlertAsync(domainEvent.Keyword, domainEvent.JobCount, domainEvent.Jobs);
    }
}
