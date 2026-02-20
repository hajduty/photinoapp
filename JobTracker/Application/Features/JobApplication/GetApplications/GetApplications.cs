using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication.GetApplications;

public record GetApplicationsResponse(List<JobApplication> AppliedJobs);

public class GetApplications : RpcHandler<NoRequest, GetApplicationsResponse>
{
    public override string Command => "jobApplication.get";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetApplications(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetApplicationsResponse> HandleAsync(NoRequest _)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return new GetApplicationsResponse(await db.JobApplications.ToListAsync());
    }
}