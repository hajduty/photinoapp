using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using System.CodeDom;

namespace JobTracker.Application.Features.Jobs.GetMatchingJobs;

public record GetMatchingJobsResponse(List<ExtendedPosting> Jobs);

public record GetMatchingJobsRequest(int Page);

public class GetMatchingJobsHandler : RpcHandler<GetMatchingJobsRequest, GetMatchingJobsResponse>
{
    public override string Command => "jobs.getMatchingJobs";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetMatchingJobsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetMatchingJobsResponse> HandleAsync(GetMatchingJobsRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.FirstOrDefaultAsync();

        throw new NotImplementedException();
    }
}
