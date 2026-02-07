using JobTracker.Application.Features.Postings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.JobSearch.GetJobs;
[ExportTsInterface]
public record GetJobsRequest(string Keyword, int Page, int PageSize, List<int> ActiveTagIds);
[ExportTsInterface]
public record GetJobsResponse(List<ExtendedPosting> Postings, int Page, int PageSize, int TotalResults, int TotalPages, bool HasPreviousPage, bool HasNextPage);

[ExportTsInterface]
public record ExtendedPosting
{
    public Posting Posting { get; init; }
    public List<Tag> Tags { get; init; }
}

public class GetJobs
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public GetJobs(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // Fetch jobs from db with pagination and filtering by tags
    public async Task<GetJobsResponse> ExecuteAsync(GetJobsRequest req)
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

        // Get all our tags from db
        var allTags = await db.Tags
            .AsNoTracking()
            .ToListAsync();

        // Get only the tags we want to filter by
        var wantedTags = allTags.Where(t => req.ActiveTagIds.Contains(t.Id)).ToList();
        var tagNames = wantedTags.Select(t => t.Name).ToList();

        // Filter postings by wanted tags using EF.Functions.Like for case-insensitive matching
        var tagFilteredQuery = query;
        foreach (var tagName in tagNames)
        {
            var tagPattern = $"%{tagName}%";
            tagFilteredQuery = tagFilteredQuery.Where(p =>
                EF.Functions.Like(p.Title, tagPattern) ||
                EF.Functions.Like(p.Description, tagPattern)
            );
        }

        var totalResults = await tagFilteredQuery.CountAsync();

        int page = Math.Max(req.Page, 1);
        int pageSize = Math.Clamp(req.PageSize, 1, 100);

        // Pagination
        var postings = await tagFilteredQuery
            .OrderByDescending(p => p.PostedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var extendedPostings = postings.Select(p => new ExtendedPosting
        {
            Posting = p,
            Tags = allTags.Where(t =>
                p.Title.Contains(t.Name, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(t.Name, StringComparison.OrdinalIgnoreCase)
            ).ToList()
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalResults / pageSize);

        var response = new GetJobsResponse(
            extendedPostings,
            page,
            pageSize,
            totalResults,
            totalPages,
            page > 1,
            page < totalPages
        );

        return response;
    }
}
