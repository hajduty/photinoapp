using JobTracker.Application.Infrastructure.Events;
using Microsoft.Extensions.Hosting;

namespace JobTracker.Application.Infrastructure.Services;

public class BackgroundWorker : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private readonly IEventEmitter _events;
    private readonly TrackerService _trackerService;
    private readonly EmbeddingService _embeddingService;

    public BackgroundWorker(IServiceProvider serviceProvider, IEventEmitter events, TrackerService trackerService, EmbeddingService embeddingService)
    {
        _events = events;
        _trackerService = trackerService;
        _embeddingService = embeddingService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _trackerService.Run();

            await _embeddingService.GenerateEmbeddingsAsync();

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
