using JobTracker.Application.Infrastructure.RPC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;

namespace JobTracker.Application.Infrastructure.BackgroundJobs;

public class JobAlertWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public JobAlertWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
                    command = "jobAlerts.process",
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
                }
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
