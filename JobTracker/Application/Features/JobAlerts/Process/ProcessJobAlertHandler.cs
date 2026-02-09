using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace JobTracker.Application.Features.JobAlerts.Process;

public class ProcessJobAlertHandler 
    : RpcHandler<object?, object?>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IServiceProvider _serviceProvider;

    public override string Command => "jobAlerts.process";

    public ProcessJobAlertHandler(IDbContextFactory<AppDbContext> dbFactory, IServiceProvider serviceProvider)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
    }

    protected async override Task<object?> HandleAsync(object? request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var alerts = await dbContext.JobAlerts.AsNoTracking().ToListAsync();

        // Resolve RpcDispatcher from service provider to avoid circular dependency
        var dispatcher = _serviceProvider.GetRequiredService<RpcDispatcher>();

        foreach (var alert in alerts)
        {
            Console.WriteLine($"Processing alert for keyword: {alert.Keyword}");
            
            // Fetch jobs for this alert's keyword
            var loadJobsRequest = JsonSerializer.Serialize(new
            {
                command = "jobSearch.loadJobs",
                id = Guid.NewGuid().ToString(),
                payload = new { keyword = alert.Keyword }
            });

            try
            {
                var result = await dispatcher.DispatchAsync(loadJobsRequest);
                Console.WriteLine($"Fetched jobs for '{alert.Keyword}': {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch jobs for '{alert.Keyword}': {ex.Message}");
            }
        }

        return null;
    }
}
