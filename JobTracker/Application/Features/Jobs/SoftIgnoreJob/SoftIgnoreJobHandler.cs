using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Jobs.SoftIgnoreJob;

public record SoftIgnoreJobRequest(int JobId);
public record SoftIgnoreJobResponse(bool Success);

public class SoftIgnoreJobHandler : RpcHandler<SoftIgnoreJobRequest, SoftIgnoreJobResponse>
{
    public override string Command => "jobs.softIgnore";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SoftIgnoreJobHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<SoftIgnoreJobResponse> HandleAsync(SoftIgnoreJobRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new SoftIgnoreJobResponse(false);

        var profileId = settings.ActiveProfileId.Value;

        var existing = await db.ProfileIgnoredJobs
            .FirstOrDefaultAsync(pij => pij.ProfileId == profileId && pij.PostingId == request.JobId);

        if (existing == null)
        {
            // Not ignored at all — soft-ignore it
            db.ProfileIgnoredJobs.Add(new ProfileIgnoredJob
            {
                ProfileId = profileId,
                PostingId = request.JobId,
                SoftIgnore = true,
                IgnoredAt = DateTime.UtcNow,
            });
        }
        else if (existing.SoftIgnore)
        {
            // Already soft-ignored — remove it
            db.ProfileIgnoredJobs.Remove(existing);
        }
        // If hard-ignored (SoftIgnore = false), leave it alone

        await db.SaveChangesAsync();
        return new SoftIgnoreJobResponse(true);
    }
}
