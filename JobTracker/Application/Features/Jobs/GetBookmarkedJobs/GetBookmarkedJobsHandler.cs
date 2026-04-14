using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Features.JobSearch.GetBookmarkedJobs;

public record GetBookmarkedJobsResponse(List<ExtendedPosting> TaggedPostings);

public class GetBookmarkedJobsHandler : RpcHandler<object?, GetBookmarkedJobsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobs.getBookmarked";

    public GetBookmarkedJobsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetBookmarkedJobsResponse> HandleAsync(object? request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new GetBookmarkedJobsResponse([]);

        var profileId = settings.ActiveProfileId.Value;

        var bookmarkedPostingIds = await db.ProfileBookmarkedJobs
            .AsNoTracking()
            .Where(b => b.ProfileId == profileId)
            .Select(b => b.PostingId)
            .ToListAsync();

        var postings = await db.Postings
            .AsNoTracking()
            .Where(p => bookmarkedPostingIds.Contains(p.Id))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        foreach (var p in postings)
            p.Bookmarked = true;

        var tags = await db.Tags.ToListAsync();

        var extendedPostings = postings.Select(p => new ExtendedPosting
        {
            Posting = p,
            Tags = tags.Where(t =>
            {
                var tagPattern = CreateTagPattern(t.Name);
                var regex = new Regex(tagPattern, RegexOptions.IgnoreCase);
                return (p.Title != null && regex.IsMatch(p.Title)) ||
                       (p.Description != null && regex.IsMatch(p.Description));
            }).ToList()
        }).ToList();

        return new GetBookmarkedJobsResponse(extendedPostings);
    }

    private string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""""])({escaped})(?:$|[\s,;.!?()\[\]{{}}""""])";
    }
}
