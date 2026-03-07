using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication.GetApplications;

public record GetApplicationsResponse(List<JobApplication> AppliedJobs);

public class GetApplicationsHandler : RpcHandler<NoRequest, List<JobApplication>>
{
    public override string Command => "applications.get";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetApplicationsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<List<JobApplication>> HandleAsync(NoRequest _)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.JobApplications
            .Include(j => j.Posting)
            .Include(j => j.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .ToListAsync();
    }
}