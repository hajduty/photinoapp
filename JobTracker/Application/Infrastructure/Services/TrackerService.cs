using JobTracker.Application.Events;
using JobTracker.Application.Features.Jobs;
using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.JobTracker;
using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Infrastructure.Services;

public class TrackerService
{
    private readonly ScrapeService _scrapeService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly JobMatchingService _matchingService;

    private const float HighMatchThreshold = 0.30f;

    public TrackerService(
        ScrapeService scrapeService,
        IDbContextFactory<AppDbContext> dbFactory,
        IEventPublisher eventPublisher,
        JobMatchingService matchingService)
    {
        _scrapeService = scrapeService;
        _dbFactory = dbFactory;
        _eventPublisher = eventPublisher;
        _matchingService = matchingService;
    }

    public async Task<int> Run(bool skipTime = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        var trackers = await db.JobTrackers.Include(j => j.Tags).ToListAsync();
        var now = DateTime.UtcNow;

        foreach (var tracker in trackers.Where(t => skipTime || t.LastCheckedAt.AddHours(t.CheckIntervalHours) <= now))
        {
            await _scrapeService.Fetch(new LoadJobsRequest(tracker.Keyword));

            var newPostings = await GetNewPostingsAsync(db, tracker);

            tracker.LastCheckedAt = DateTime.UtcNow;

            if (newPostings.Count == 0)
                continue;

            await PublishTrackingAlertAsync(tracker, newPostings);

            if (settings != null)
                await PublishHighMatchAlertsAsync(db, settings, tracker, newPostings);
        }

        await db.SaveChangesAsync();
        return 0;
    }

    private async Task<List<Posting>> GetNewPostingsAsync(AppDbContext db, Features.JobTracker.JobTracker tracker)
    {
        var escapedKeyword = tracker.Keyword
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");

        var pattern = $"%{escapedKeyword}%";

        var query = db.Postings
            .AsNoTracking()
            .Where(p => p.CreatedAt > tracker.LastCheckedAt)
            .Where(p =>
                EF.Functions.Like(p.Title, pattern) ||
                EF.Functions.Like(p.Company, pattern) ||
                EF.Functions.Like(p.Description, pattern));

        var postings = await query.ToListAsync();

        foreach (var tag in tracker.Tags)
        {
            var regex = new Regex(CreateTagPattern(tag.Name), RegexOptions.IgnoreCase);
            postings = postings
                .Where(p =>
                    (p.Title != null && regex.IsMatch(p.Title)) ||
                    (p.Description != null && regex.IsMatch(p.Description)))
                .ToList();
        }

        return postings;
    }

    private async Task PublishTrackingAlertAsync(Features.JobTracker.JobTracker tracker, List<Posting> newPostings)
    {
        var jobsInfo = newPostings
            .Select(p => new JobInfo(p.Id, p.Title, p.Company, p.OriginUrl, p.CompanyImage))
            .ToList();

        await _eventPublisher.PublishAsync(new JobsFoundEvent(
            tracker.Id,
            tracker.Keyword,
            newPostings.Count,
            jobsInfo));
    }

    private async Task PublishHighMatchAlertsAsync(
        AppDbContext db,
        Settings settings,
        Features.JobTracker.JobTracker tracker,
        List<Posting> newPostings)
    {
        var newIds = newPostings.Select(p => p.Id).ToHashSet();

        var highMatches = (await _matchingService.GetScoredJobsAsync(db, settings))
            .Where(x => newIds.Contains(x.Posting.Id) && x.Score >= HighMatchThreshold)
            .ToList();

        if (highMatches.Count == 0)
            return;

        var matchInfos = highMatches
            .Select(x => new JobInfo(x.Posting.Id, x.Posting.Title, x.Posting.Company, x.Posting.OriginUrl, x.Posting.CompanyImage))
            .ToList();

        await _eventPublisher.PublishAsync(new HighMatchJobsFoundEvent(
            tracker.Id,
            tracker.Keyword,
            highMatches.Count,
            matchInfos));
    }

    private static string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""'`])({escaped})(?:$|[\s,;.!?()\[\]{{}}""'`])";
    }
}
