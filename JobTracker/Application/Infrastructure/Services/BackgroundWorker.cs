using JobTracker.Application.Events;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace JobTracker.Application.Infrastructure.Services;

public class BackgroundWorker : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private readonly IUiEventEmitter _events;
    private readonly TrackerService _trackerService;
    private readonly EmbeddingService _embeddingService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BackgroundWorker(IServiceProvider serviceProvider, IUiEventEmitter events, TrackerService trackerService, EmbeddingService embeddingService, IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _events = events;
        _trackerService = trackerService;
        _embeddingService = embeddingService;
        _dbFactory = dbContextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.Settings.FirstOrDefaultAsync();

            await _trackerService.Run();

            if (settings != null && settings.GenerateEmbeddings)
            {
                await _embeddingService.GenerateEmbeddingsAsync();
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
