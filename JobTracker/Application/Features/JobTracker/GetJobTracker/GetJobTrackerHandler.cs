using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobTracker.GetJobTracker;

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

        var settings = await dbContext.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return [];

        return await dbContext.JobTrackers
            .Where(j => j.ProfileId == settings.ActiveProfileId.Value)
            .Include(j => j.Tags)
            .ToListAsync();
    }
}
