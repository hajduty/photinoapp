using JobTracker.Application.Features.JobTracker.Process;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Infrastructure.Services;

public class TrackerService
{
    private readonly ScrapeService _scrapeService;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEventPublisher _eventPublisher;

    public TrackerService(ScrapeService scrapeService, IDbContextFactory<AppDbContext> dbFactory, IEventPublisher eventPublisher)
    {
        _scrapeService = scrapeService;
        _dbFactory = dbFactory;
        _eventPublisher = eventPublisher;
    }

    public async Task<int> Run()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var trackedKeywords = await db.JobTrackers.Include(j => j.Tags).ToListAsync();

        int totalJobsAdded = 0;

        foreach (var track in trackedKeywords)
        {
            await _scrapeService.Fetch(new Features.JobSearch.LoadJobs.LoadJobsRequest(track.Keyword));

            var escapedKeyword = track.Keyword
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");

            var pattern = $"%{escapedKeyword}%";

            var query = db.Postings
                .AsNoTracking()
                .Where(p => p.CreatedAt > track.LastCheckedAt) // disable for now (testing)
                .Where(p =>
                    EF.Functions.Like(p.Title, pattern) ||
                    EF.Functions.Like(p.Company, pattern) ||
                    EF.Functions.Like(p.Description, pattern));

            var allTags = await db.Tags.AsNoTracking().ToListAsync();
            var wantedTags = track.Tags;
            var tagNames = wantedTags.Select(t => t.Name).ToList();

            var newPostings = await query.ToListAsync();

            foreach (var tagName in tagNames)
            {
                var patterns = CreateTagPattern(tagName);
                var regex = new Regex(patterns, RegexOptions.IgnoreCase);
                newPostings = newPostings.Where(p =>
                    (p.Title != null && regex.IsMatch(p.Title)) ||
                    (p.Description != null && regex.IsMatch(p.Description))
                ).ToList();
            }

            track.LastCheckedAt = DateTime.UtcNow;

            // Publish domain event only when NEW jobs are found
            if (newPostings.Any())
            {
                var jobsInfo = newPostings.Select(p => new JobInfo(p.Id, p.Title, p.Company)).ToList();
                await _eventPublisher.PublishAsync(new JobsFoundEvent(
                    track.Id,
                    track.Keyword,
                    newPostings.Count,
                    jobsInfo
                ));
            }
        }

        await db.SaveChangesAsync();

        return totalJobsAdded;
    }

    private string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""""])({escaped})(?:$|[\s,;.!?()\[\]{{}}""""])";
    }
}
