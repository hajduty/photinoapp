using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Events;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;

namespace JobTracker.Application.Infrastructure.BackgroundJobs;

public class JobTrackerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private readonly IEventEmitter _events;

    public JobTrackerWorker(IServiceProvider serviceProvider, IEventEmitter events)
    {
        _serviceProvider = serviceProvider;
        _events = events;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dispatcher = scope.ServiceProvider.GetRequiredService<RpcDispatcher>();

                var request = JsonSerializer.Serialize(new
                {
                    command = "jobTracker.process",
                    id = Guid.NewGuid().ToString(),
                    payload = new { }
                });
                
                try
                {
                    await dispatcher.DispatchAsync(request);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing job alerts: {ex}");

                    _events.Emit("jobTracker:error", new
                    {
                        ErrorName = "JobTrackerWorker",
                        Error = ex.ToString()
                    });
                }
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
