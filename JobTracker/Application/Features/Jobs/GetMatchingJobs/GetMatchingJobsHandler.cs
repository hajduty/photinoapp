using JobTracker.Application.Features.Jobs.GetJobs;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Jobs.GetMatchingJobs;

public record GetMatchingJobsRequest(int Page);

public record GetMatchingJobsResponse(List<ExtendedPosting> Jobs);

public class GetMatchingJobsHandler : RpcHandler<GetMatchingJobsRequest, GetMatchingJobsResponse>
{
    public override string Command => "jobs.getMatchingJobs";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly JobMatchingService _matchingService;

    public GetMatchingJobsHandler(
        IDbContextFactory<AppDbContext> dbFactory,
        JobMatchingService matchingService)
    {
        _dbFactory = dbFactory;
        _matchingService = matchingService;
    }

    protected override async Task<GetMatchingJobsResponse> HandleAsync(GetMatchingJobsRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new GetMatchingJobsResponse([]);

        var profile = await db.JobProfiles
            .AsNoTracking()
            .Include(p => p.SelectedTags)
            .FirstOrDefaultAsync(p => p.Id == settings.ActiveProfileId);

        if (profile == null)
            return new GetMatchingJobsResponse([]);

        var scored = await _matchingService.GetScoredJobsAsync(db, profile);

        var bookmarkedIds = (await db.ProfileBookmarkedJobs
            .AsNoTracking()
            .Where(b => b.ProfileId == profile.Id)
            .Select(b => b.PostingId)
            .ToListAsync()).ToHashSet();

        var extended = scored
            .Take(15)
            .Select(x =>
            {
                x.Posting.Bookmarked = bookmarkedIds.Contains(x.Posting.Id);
                return new ExtendedPosting { Posting = x.Posting, Tags = x.Tags };
            })
            .ToList();

        return new GetMatchingJobsResponse(extended);
    }
}
