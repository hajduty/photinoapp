using JobTracker.Application.Events;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.SemanticSearch;

public record EmbeddingsCancelled(
    int EmbeddingsGenerated
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class EmbeddingsCancelledHandler : IEventHandler<EmbeddingsCancelled>
{
    private readonly IUiEventEmitter _uiEventEmitter;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EmbeddingsCancelledHandler(IUiEventEmitter uiEventEmitter, IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _uiEventEmitter = uiEventEmitter;
    }

    public async Task HandleAsync(EmbeddingsCancelled domainEvent)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var notification = new Notification.Notification
        {
            Title = "Embeddings Worker",
            Description = "Embeddings background service cancelled by user",
            Type = NotificationType.EmbeddingsEvent,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        _uiEventEmitter.Emit("embeddings.error", notification);
    }
}
