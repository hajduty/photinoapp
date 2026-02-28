using JobTracker.Application.Events;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.SemanticSearch;

public record EmbeddingsFinished(
    int EmbeddingsGenerated
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class EmbeddingsFinishedHandler : IEventHandler<EmbeddingsFinished>
{
    private readonly IUiEventEmitter _uiEventEmitter;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EmbeddingsFinishedHandler(IUiEventEmitter uiEventEmitter, IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _uiEventEmitter = uiEventEmitter;
    }

    public async Task HandleAsync(EmbeddingsFinished domainEvent)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var notification = new Notification.Notification
        {
            CreatedAt = DateTime.UtcNow,
            Description = $"Embeddings service finished, {domainEvent.EmbeddingsGenerated} jobs processed.",
            Title = "Embeddings Generation Finished",
            Type = NotificationType.EmbeddingsEvent
        };

        //db.Notifications.Add(notification);
        //await db.SaveChangesAsync();

        _uiEventEmitter.Emit("embeddings.completed", notification);
    }
}
