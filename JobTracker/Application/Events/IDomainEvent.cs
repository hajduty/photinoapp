namespace JobTracker.Application.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
}
