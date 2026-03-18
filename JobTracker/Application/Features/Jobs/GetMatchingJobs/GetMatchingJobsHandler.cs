using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Embeddings;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Features.JobSearch.GetMatchingJobs;

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
        if (settings == null)
            return new GetMatchingJobsResponse([]);

        var scored = await _matchingService.GetScoredJobsAsync(db, settings);

        var extended = scored
            .Take(15)
            .Select(x => new ExtendedPosting { Posting = x.Posting, Tags = x.Tags })
            .ToList();

        return new GetMatchingJobsResponse(extended);
    }
}