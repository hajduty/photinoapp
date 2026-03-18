using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Jobs.GetIgnoredJobs;

public record GetIgnoredJobsResponse(List<Posting> Jobs);
internal class GetIgnoredJobsHandler : RpcHandler<NoRequest, GetIgnoredJobsResponse>
{
    public override string Command => "jobs.getIgnored";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetIgnoredJobsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetIgnoredJobsResponse> HandleAsync(NoRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var jobs = await db.Postings.Where(p => p.Ignored == true).ToListAsync();

        return new GetIgnoredJobsResponse(jobs);
    }
}
