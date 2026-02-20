using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication.GetApplications;

public record GetApplicationsResponse(List<JobApplication> AppliedJobs);

public class GetApplicationsHandler : RpcHandler<NoRequest, GetApplicationsResponse>
{
    public override string Command => "applications.get";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetApplicationsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetApplicationsResponse> HandleAsync(NoRequest _)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return new GetApplicationsResponse(await db.JobApplications.Include(j => j.Posting).ToListAsync());
    }
}