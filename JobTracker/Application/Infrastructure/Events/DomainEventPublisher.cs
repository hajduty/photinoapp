using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Application.Infrastructure.Events;

public sealed class DomainEventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<T>(T domainEvent) where T : IDomainEvent
    {
        var handlers = _serviceProvider.GetServices<IEventHandler<T>>();
        
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent);
        }
    }
}
