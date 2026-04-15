using JobTracker.Application.Features.Jobs;
using JobTracker.Application.Features.Profiles;
using JobTracker.Application.Features.Settings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

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

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new IgnoreJobResponse(false);

        var profileId = settings.ActiveProfileId.Value;

        var existing = await db.ProfileIgnoredJobs
            .FirstOrDefaultAsync(pij => pij.ProfileId == profileId && pij.PostingId == request.JobId);

        if (existing != null)
        {
            // Toggle off — unignore
            var wasReason = existing.Reason;
            db.ProfileIgnoredJobs.Remove(existing);

            if (wasReason.HasValue)
            {
                var profile = await db.JobProfiles.FirstOrDefaultAsync(p => p.Id == profileId);
                if (profile != null)
                {
                    RemoveFromProfileAggregates(posting, wasReason.Value, profile);
                    await db.SaveChangesAsync();
                    return new IgnoreJobResponse(true);
                }
            }
        }
        else
        {
            // Ignore
            var allTags = await db.Tags.AsNoTracking().ToListAsync();
            var profile = await db.JobProfiles.FirstOrDefaultAsync(p => p.Id == profileId);

            var ageDays = (DateTime.UtcNow - posting.PostedDate).TotalDays;

            var ignoredJob = new ProfileIgnoredJob
            {
                ProfileId = profileId,
                PostingId = request.JobId,
                IgnoredAt = DateTime.UtcNow,
            };

            if (ageDays > 45)
            {
                ignoredJob.SoftIgnore = true;
                ignoredJob.Reason = IgnoreReason.Requirements;
            }
            else
            {
                ignoredJob.Reason = request.Reason ?? IgnoreReason.Requirements;

                if (profile != null)
                    UpdateProfileAggregates(posting, request.Reason, request.RejectedTags, profile);
            }

            db.ProfileIgnoredJobs.Add(ignoredJob);
        }

        await db.SaveChangesAsync();
        return new IgnoreJobResponse(true);
    }

    private static void UpdateProfileAggregates(
        Posting posting,
        IgnoreReason? reason,
        List<int>? rejectedTags,
        JobProfile profile)
    {
        switch (reason)
        {
            case IgnoreReason.Location:
                if (!string.IsNullOrWhiteSpace(posting.City))
                {
                    profile.BlockedLocations ??= [];
                    if (!profile.BlockedLocations.Contains(posting.City, StringComparer.OrdinalIgnoreCase))
                        profile.BlockedLocations.Add(posting.City);
                }
                break;

            case IgnoreReason.Experience:
                var seniorityLevel = DetectSeniorityLevel(posting);
                if (seniorityLevel != null)
                {
                    profile.RejectedSeniorityLevels ??= [];
                    if (!profile.RejectedSeniorityLevels.Contains(seniorityLevel, StringComparer.OrdinalIgnoreCase))
                        profile.RejectedSeniorityLevels.Add(seniorityLevel);
                }
                break;

            case IgnoreReason.Tags:
                profile.RejectedTechKeywords ??= [];
                if (rejectedTags != null)
                {
                    foreach (var tagId in rejectedTags)
                    {
                        if (!profile.RejectedTechKeywords.Any(r => r.TagId == tagId))
                            profile.RejectedTechKeywords.Add(new RejectedTagRule(tagId, KeywordScope.Both));
                    }
                }
                break;
        }
    }

    private static void RemoveFromProfileAggregates(Posting posting, IgnoreReason reason, JobProfile profile)
    {
        switch (reason)
        {
            case IgnoreReason.Location:
                if (!string.IsNullOrWhiteSpace(posting.City))
                    profile.BlockedLocations?.RemoveAll(l => string.Equals(l, posting.City, StringComparison.OrdinalIgnoreCase));
                break;

            case IgnoreReason.Experience:
                var seniorityLevel = DetectSeniorityLevel(posting);
                if (seniorityLevel != null)
                    profile.RejectedSeniorityLevels?.RemoveAll(s => string.Equals(s, seniorityLevel, StringComparison.OrdinalIgnoreCase));
                break;
        }
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
