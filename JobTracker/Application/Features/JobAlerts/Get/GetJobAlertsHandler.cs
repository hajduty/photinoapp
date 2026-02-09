using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobAlerts.Get;

public sealed class GetJobAlertsHandler : RpcHandler<object?, List<JobAlert>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobAlerts.getAlerts";

    public GetJobAlertsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<List<JobAlert>> HandleAsync(object? request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        return await dbContext.JobAlerts.ToListAsync();
    }
}
