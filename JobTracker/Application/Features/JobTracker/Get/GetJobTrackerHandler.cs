using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobTracker.Get;

public sealed class GetJobTrackerHandler : RpcHandler<object?, List<JobTracker>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobTracker.getTrackers";

    public GetJobTrackerHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<List<JobTracker>> HandleAsync(object? request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        return await dbContext.JobTrackers.Include(j => j.Tags).ToListAsync();
    }
}
