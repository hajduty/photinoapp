using JobTracker.Application.Features.Postings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.JobSearch.GetJobs;

public record GetJobsRequest(string Keyword, int Page, int PageSize, List<int> ActiveTagIds, DateTime? TimeSinceUpload);

public record GetJobsResponse(List<ExtendedPosting> Postings, int Page, int PageSize, int TotalResults, int TotalPages, bool HasPreviousPage, bool HasNextPage);

public record ExtendedPosting
{
    public Posting Posting { get; init; }
    public List<Tag> Tags { get; init; }
}

public class GetJobsHandler : RpcHandler<GetJobsRequest, GetJobsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobSearch.getJobs";

    public GetJobsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetJobsResponse> HandleAsync(GetJobsRequest req)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var escapedKeyword = req.Keyword
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");

        var pattern = $"%{escapedKeyword}%";

        var query = db.Postings
            .AsNoTracking()
            .Where(p =>
                EF.Functions.Like(p.Title, pattern) ||
                EF.Functions.Like(p.Company, pattern) ||
                EF.Functions.Like(p.Description, pattern));

        if (req.TimeSinceUpload.HasValue)
        {
            query = query.Where(p => p.PostedDate >= req.TimeSinceUpload.Value);
        }

        var allTags = await db.Tags.AsNoTracking().ToListAsync();
        var wantedTags = allTags.Where(t => req.ActiveTagIds.Contains(t.Id)).ToList();
        var tagNames = wantedTags.Select(t => t.Name).ToList();

        var postings = await query.ToListAsync();

        foreach (var tagName in tagNames)
        {
            var patterns = CreateTagPattern(tagName);
            var regex = new Regex(patterns, RegexOptions.IgnoreCase);
            postings = postings.Where(p =>
                (p.Title != null && regex.IsMatch(p.Title)) ||
                (p.Description != null && regex.IsMatch(p.Description))
            ).ToList();
        }

        var totalResults = postings.Count;

        int page = Math.Max(req.Page, 1);
        int pageSize = Math.Clamp(req.PageSize, 1, 100);

        postings = postings
            .OrderByDescending(p => p.PostedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var extendedPostings = postings.Select(p => new ExtendedPosting
        {
            Posting = p,
            Tags = allTags.Where(t =>
            {
                var tagPattern = CreateTagPattern(t.Name);
                var regex = new Regex(tagPattern, RegexOptions.IgnoreCase);
                return (p.Title != null && regex.IsMatch(p.Title)) ||
                       (p.Description != null && regex.IsMatch(p.Description));
            }).ToList()
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalResults / pageSize);

        return new GetJobsResponse(
            extendedPostings,
            page,
            pageSize,
            totalResults,
            totalPages,
            page > 1,
            page < totalPages
        );
    }

    private string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""""])({escaped})(?:$|[\s,;.!?()\[\]{{}}""""])";
    }
}
