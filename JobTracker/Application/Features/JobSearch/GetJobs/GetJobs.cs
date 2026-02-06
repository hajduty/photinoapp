/* using JobTracker.Application.Features.Postings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JobTracker.Application.Features.JobSearch.GetJobs;

public record GetJobsRequest(
    string Keyword,
    string? Source = null,
    string? DateFilter = null,
    string? Tags = null,
    int Page = 1,
    int PageSize = 20
);

public record GetJobsResponse(
    List<Posting> Jobs,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage
);

public record PostingWithTags
{
    public List<Tag> Tags { get; set; } = new();
    public Posting Posting { get; set; } = null!;
}

public class GetJobs
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly HttpClient _httpClient;
    private readonly string apiUrl = "https://jobsearch.api.jobtechdev.se/";

    private static readonly ConcurrentDictionary<string, Task> _activeBackfills = new();

    public GetJobs(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _httpClient = new HttpClient();
    }

    public async Task<GetJobsResponse> ExecuteAsync(GetJobsRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Trigger backfill in background (fire-and-forget)
        _ = BackfillForKeyword(request.Keyword);

        // Query from database with proper filtering
        var query = db.Postings.AsQueryable();

        // Apply keyword search (full-text search if available)
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(p =>
                p.Title.Contains(request.Keyword) ||
                p.Description.Contains(request.Keyword));
        }

        // Apply date filter
        if (!string.IsNullOrWhiteSpace(request.DateFilter))
        {
            if (DateTime.TryParse(request.DateFilter, out var filterDate))
            {
                query = query.Where(p => p.PostedDate >= filterDate);
            }
        }

        // Apply tag filtering
        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            query = ApplyTagFilter(query, request.Tags);
        }

        // Apply source filter
        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            query = query.Where(p => p.Source == request.Source);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderByDescending(p => p.PostedDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        Debug.WriteLine(request.Tags);

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new GetJobsResponse(
            Jobs: items,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            HasPreviousPage: request.Page > 1,
            HasNextPage: request.Page < totalPages
        );
    }

    private async Task BackfillForKeyword(string keyword)
    {
        // Ensure only one backfill per keyword runs at a time
        var backfillKey = $"backfill:{keyword}";

        if (_activeBackfills.ContainsKey(backfillKey))
            return;

        var backfillTask = BackfillInternal(keyword);
        _activeBackfills[backfillKey] = backfillTask;

        try
        {
            await backfillTask;
        }
        finally
        {
            _activeBackfills.TryRemove(backfillKey, out _);
        }
    }

    private async Task BackfillInternal(string keyword)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        int apiOffset = 0;
        const int pageSize = 100; // Max allowed by API
        bool foundExisting = false;

        while (!foundExisting)
        {
            var query = $"search?q={keyword}&offset={apiOffset}&limit={pageSize}&sort=pubdate-desc";
            var responseString = await _httpClient.GetStringAsync(apiUrl + query);

            var apiResponse = JobSearchToPosting.Convert(responseString);

            if (!apiResponse.Jobs.Any())
                break;

            foreach (var posting in apiResponse.Jobs)
            {
                // Check if posting already exists in DB
                var exists = await db.Postings.AnyAsync(p => p.Id == posting.Id);

                if (exists)
                {
                    foundExisting = true;
                    break; // Stop when we reach already-synced content
                }

                // Add to database
                db.Postings.Add(posting);
            }

            await db.SaveChangesAsync();

            // Stop if we found existing items or reached end
            if (foundExisting || apiResponse.Jobs.Count < pageSize)
                break;

            apiOffset += pageSize;

            // Rate limiting
            await Task.Delay(100);
        }
    }


    // Helpers
    private IQueryable<Posting> ApplyTagFilter(IQueryable<Posting> query, string tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return query;

        var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // For OR logic (job contains ANY of the tags)
        // This is translatable to SQL: WHERE Description LIKE '%tag1%' OR Description LIKE '%tag2%'

        // Start with a false predicate and build OR conditions
        var predicate = PredicateBuilder.False<Posting>();
        foreach (var tag in tagList)
        {
            var localTag = tag; // Capture variable for closure
            predicate = predicate.Or(p => p.Description != null && p.Description.Contains(localTag));
        }

        return query.Where(predicate);
    }

    // Apply general tags
    private bool MatchesDate(Posting p, string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return true;
        if (!DateTime.TryParse(dateStr, out var date)) return true;
        return p.PostedDate >= date;
    }
}

*/