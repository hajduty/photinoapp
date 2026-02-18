using JobTracker.Application.Events;
using JobTracker.Application.Features.Notification;
using System.Security.Cryptography.X509Certificates;

namespace JobTracker.Application.Features.SemanticSearch;

public record EmbeddingsProgress(
    int EmbeddingsLeft,
    int EmbeddingsProcessed
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class EmbeddingsProgressHandler : IEventHandler<EmbeddingsProgress>
{
    private readonly IUiEventEmitter _uiEventEmitter;

    public EmbeddingsProgressHandler(IUiEventEmitter uiEventEmitter)
    {
        _uiEventEmitter = uiEventEmitter;
    }

    public Task HandleAsync(EmbeddingsProgress domainEvent)
    {
        var notification = new Notification.Notification
        {
            CreatedAt = DateTime.UtcNow,
            Title = "Generating embeddings for job postings",
            Description = $"{domainEvent.EmbeddingsProcessed}/{domainEvent.EmbeddingsLeft} embeddings generated..",
            Type = NotificationType.EmbeddingsEvent
        };

        _uiEventEmitter.Emit("embeddings.generating", notification);
        return Task.CompletedTask;
    }
}