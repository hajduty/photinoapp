using JobTracker.Application.Features.JobTracker;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Discord;
using JobTracker.Application.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Notification;

public class JobsFoundEventHandler : IEventHandler<JobsFoundEvent>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEventEmitter _eventEmitter;
    private readonly IDiscordWebhookService _discord;

    public JobsFoundEventHandler(
        IDbContextFactory<AppDbContext> dbFactory,
        IEventEmitter eventEmitter,
        IDiscordWebhookService discord)
    {
        _dbFactory = dbFactory;
        _eventEmitter = eventEmitter;
        _discord = discord;
    }

    public async Task HandleAsync(JobsFoundEvent domainEvent)
    {
        var title = domainEvent.JobCount == 1
            ? "1 new job found!"
            : $"{domainEvent.JobCount} new jobs found!";

        var description = domainEvent.JobCount == 1
            ? $"A new job matching '{domainEvent.Keyword}' was found."
            : $"{domainEvent.JobCount} new jobs matching '{domainEvent.Keyword}' were found.";

        // Save to database
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var notification = new Notification
        {
            Title = title,
            Description = description,
            Type = NotificationType.MatchingJob
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        // Emit to frontend
        _eventEmitter.Emit("notification.new", notification);

        // Send Discord webhook
        var jobTitles = domainEvent.Jobs.Select(j => j.Title).ToArray();
        await _discord.SendJobAlertAsync(domainEvent.Keyword, domainEvent.JobCount, jobTitles);
    }
}
