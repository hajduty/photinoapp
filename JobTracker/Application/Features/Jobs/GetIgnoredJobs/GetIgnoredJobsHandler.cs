using JobTracker.Application.Features.Jobs;
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

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new GetIgnoredJobsResponse([]);

        var profileId = settings.ActiveProfileId.Value;

        var ignoredPostingIds = await db.ProfileIgnoredJobs
            .AsNoTracking()
            .Where(pij => pij.ProfileId == profileId && !pij.SoftIgnore)
            .Select(pij => pij.PostingId)
            .ToListAsync();

        var jobs = await db.Postings
            .AsNoTracking()
            .Where(p => ignoredPostingIds.Contains(p.Id))
            .ToListAsync();

        return new GetIgnoredJobsResponse(jobs);
    }
}
