using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Features.JobSearch.GetJobs;

public record QueryJobsRequest(string Keyword, int Page, int PageSize, List<int> ActiveTagIds, DateTime? TimeSinceUpload);

public record QueryJobsResponse(List<ExtendedPosting> Postings, int Page, int PageSize, int TotalResults, int TotalPages, bool HasPreviousPage, bool HasNextPage);

public record ExtendedPosting
{
    public Posting Posting { get; init; } = null!;
    public List<Tag> Tags { get; init; } = null!;
}

public class QueryJobsHandler : RpcHandler<QueryJobsRequest, QueryJobsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobs.getJobs";

    public QueryJobsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<QueryJobsResponse> HandleAsync(QueryJobsRequest req)
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
            query = query.Where(p => p.PostedDate >= req.TimeSinceUpload.Value);

        // Fetch tags and postings in parallel
        var allTagsTask = db.Tags.AsNoTracking().ToListAsync();
        var postingsTask = query.ToListAsync();

        await Task.WhenAll(allTagsTask, postingsTask);

        var allTags = allTagsTask.Result;
        var postings = postingsTask.Result;

        // Pre-compile all regexes once
        var allTagRegexes = allTags
            .ToDictionary(t => t.Id, t => new Regex(CreateTagPattern(t.Name), RegexOptions.IgnoreCase | RegexOptions.Compiled));

        // Filter by active tags using pre-compiled regexes
        if (req.ActiveTagIds is { Count: > 0 })
        {
            var activeRegexes = req.ActiveTagIds
                .Where(allTagRegexes.ContainsKey)
                .Select(id => allTagRegexes[id])
                .ToList();

            postings = postings
                .Where(p => activeRegexes.All(rx =>
                    (p.Title != null && rx.IsMatch(p.Title)) ||
                    (p.Description != null && rx.IsMatch(p.Description))))
                .ToList();
        }

        var totalResults = postings.Count;

        int page = Math.Max(req.Page, 1);
        int pageSize = Math.Clamp(req.PageSize, 1, 100);

        var pagedPostings = postings
            .OrderByDescending(p => p.PostedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var extendedPostings = pagedPostings.Select(p =>
        {
            var matchingTags = allTags
                .Where(t =>
                {
                    var rx = allTagRegexes[t.Id];
                    return (p.Title != null && rx.IsMatch(p.Title)) ||
                           (p.Description != null && rx.IsMatch(p.Description));
                })
                .ToList();

            var descRaw = p.Description ?? "";
            var descFmt = p.DescriptionFormatted ?? "";

            return new ExtendedPosting
            {
                Posting = new Posting
                {
                    Id = p.Id,
                    Title = p.Title,
                    Company = p.Company,
                    Location = p.Location,
                    PostedDate = p.PostedDate,
                    Description = descRaw[..Math.Min(descRaw.Length, 400)],
                    DescriptionFormatted = descFmt[..Math.Min(descFmt.Length, 400)],
                    Bookmarked = p.Bookmarked,
                    CompanyImage = p.CompanyImage,
                    CreatedAt = p.CreatedAt,
                    OriginUrl = p.OriginUrl,
                    LastApplicationDate = p.LastApplicationDate,
                    YearsOfExperience = p.YearsOfExperience,
                    Source = p.Source,
                    Url = p.Url
                },
                Tags = matchingTags
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalResults / pageSize);

        return new QueryJobsResponse(
            extendedPostings,
            page,
            pageSize,
            totalResults,
            totalPages,
            page > 1,
            page < totalPages
        );
    }

    private static string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""""])({escaped})(?:$|[\s,;.!?()\[\]{{}}""""])";
    }
}