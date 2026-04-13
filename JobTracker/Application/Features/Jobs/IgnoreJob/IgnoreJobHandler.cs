using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Features.Jobs.IgnoreJob;

public record IgnoreJobRequest(int JobId, IgnoreReason? Reason = null, List<int>? RejectedTags = null);
public record IgnoreJobResponse(bool Success);

public class IgnoreJobHandler : RpcHandler<IgnoreJobRequest, IgnoreJobResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobs.ignore";

    public IgnoreJobHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<IgnoreJobResponse> HandleAsync(IgnoreJobRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var posting = await db.Postings.FirstOrDefaultAsync(p => p.Id == request.JobId);
        if (posting == null)
            return new IgnoreJobResponse(false);

        var settings = await db.Settings
            .FirstOrDefaultAsync();

        var allTags = await db.Tags.AsNoTracking().ToListAsync();

        if (posting.Ignored == true || posting.SoftIgnore == true)
        {
            await UnignoreJobAsync(db, posting, settings, allTags);
        }
        else
        {
            await IgnoreJobAsync(db, posting, settings, request.Reason, request.RejectedTags, allTags);
        }

        await db.SaveChangesAsync();
        return new IgnoreJobResponse(true);
    }

    private async Task IgnoreJobAsync(
        AppDbContext db, 
        Posting posting, 
        Settings? settings, 
        IgnoreReason? reason, 
        List<int>? rejectedTags,
        List<Tag> allTags)
    {
        var ageDays = (DateTime.UtcNow - posting.PostedDate).TotalDays;

        if (ageDays > 45)
        {
            posting.SoftIgnore = true;
            posting.Reason = IgnoreReason.Requirements;
            return;
        }

        posting.Ignored = true;
        posting.IgnoredAt = DateTime.UtcNow;
        posting.Reason = reason ?? IgnoreReason.Requirements;

        if (settings != null)
        {
            await UpdateSettingsAggregatesAsync(db, posting, reason, rejectedTags, settings, allTags);
        }
    }

    private async Task UnignoreJobAsync(
        AppDbContext db, 
        Posting posting, 
        Settings? settings,
        List<Tag> allTags)
    {
        var wasReason = posting.Reason;

        posting.Ignored = false;
        posting.SoftIgnore = false;
        posting.IgnoredAt = null;
        posting.Reason = null;

        if (wasReason.HasValue && settings != null)
        {
            await RemoveFromSettingsAggregatesAsync(db, posting, wasReason.Value, settings, allTags);
        }
    }

    private static async Task UpdateSettingsAggregatesAsync(
        AppDbContext db, 
        Posting posting, 
        IgnoreReason? reason, 
        List<int>? rejectedTags,
        Settings settings, 
        List<Tag> allTags)
    {
        switch (reason)
        {
            case IgnoreReason.Location:
                if (!string.IsNullOrWhiteSpace(posting.Location))
                {
                    settings.BlockedLocations ??= [];
                    if (!settings.BlockedLocations.Contains(posting.Location, StringComparer.OrdinalIgnoreCase))
                        settings.BlockedLocations.Add(posting.Location);
                }
                break;

            case IgnoreReason.Experience:
                var seniorityLevel = DetectSeniorityLevel(posting);
                if (seniorityLevel != null)
                {
                    settings.RejectedSeniorityLevels ??= [];
                    if (!settings.RejectedSeniorityLevels.Contains(seniorityLevel, StringComparer.OrdinalIgnoreCase))
                        settings.RejectedSeniorityLevels.Add(seniorityLevel);
                }
                break;

            case IgnoreReason.Tags:
                settings.RejectedTechKeywords ??= [];

                if (rejectedTags != null && rejectedTags.Count > 0)
                {
                    foreach (var tagId in rejectedTags)
                    {
                        if (!settings.RejectedTechKeywords.Contains(tagId))
                            settings.RejectedTechKeywords.Add(tagId);
                    }
                }
                break;
        }

        await Task.CompletedTask;
    }

    private static async Task RemoveFromSettingsAggregatesAsync(
        AppDbContext db, 
        Posting posting, 
        IgnoreReason reason, 
        Settings settings,
        List<Tag> allTags)
    {
        switch (reason)
        {
            case IgnoreReason.Location:
                if (!string.IsNullOrWhiteSpace(posting.Location))
                    settings.BlockedLocations?.RemoveAll(l => string.Equals(l, posting.Location, StringComparison.OrdinalIgnoreCase));
                break;

            case IgnoreReason.Experience:
                var seniorityLevel = DetectSeniorityLevel(posting);
                if (seniorityLevel != null)
                    settings.RejectedSeniorityLevels?.RemoveAll(s => string.Equals(s, seniorityLevel, StringComparison.OrdinalIgnoreCase));
                break;
        }

        await Task.CompletedTask;
    }

    private static string? DetectSeniorityLevel(Posting posting)
    {
        var years = posting.YearsOfExperience ?? 0;

        if (years <= 1) return "junior";
        if (years <= 4) return "mid";
        if (years <= 6) return "senior";
        return "lead";
    }
}
