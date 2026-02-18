using JobTracker.Application.Events;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.SemanticSearch;

public record EmbeddingsStarted()
    : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class EmbeddingsStartedHandler : IEventHandler<EmbeddingsStarted>
{
    private readonly IUiEventEmitter _uiEventEmitter;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public EmbeddingsStartedHandler(IDbContextFactory<AppDbContext> dbContextFactory, IUiEventEmitter uiEventEmitter)
    {
        _dbContextFactory = dbContextFactory;
        _uiEventEmitter = uiEventEmitter;
    }

    public async Task HandleAsync(EmbeddingsStarted domainEvent)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var notification = new Notification.Notification
        {
            CreatedAt = DateTime.UtcNow,
            Title = "Embeddings Service Started",
            Description = "Embeddings generation started in the background",
            Type = NotificationType.EmbeddingsEvent
        };

        _uiEventEmitter.Emit("embeddings.started", notification);
    }
}