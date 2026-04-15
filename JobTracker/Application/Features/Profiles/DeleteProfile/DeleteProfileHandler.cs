using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Profiles.DeleteProfile;

public record DeleteProfileRequest(int ProfileId);
public record DeleteProfileResponse(bool Success, string? Error);

public class DeleteProfileHandler : RpcHandler<DeleteProfileRequest, DeleteProfileResponse>
{
    public override string Command => "profiles.delete";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DeleteProfileHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<DeleteProfileResponse> HandleAsync(DeleteProfileRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var totalProfiles = await db.JobProfiles.CountAsync();
        if (totalProfiles <= 1)
            return new DeleteProfileResponse(false, "Cannot delete the last profile.");

        var profile = await db.JobProfiles.FindAsync(request.ProfileId);
        if (profile == null)
            return new DeleteProfileResponse(false, "Profile not found.");

        // If this profile is active, switch to another one first
        var settings = await db.Settings.FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == request.ProfileId)
        {
            var other = await db.JobProfiles
                .Where(p => p.Id != request.ProfileId)
                .Select(p => p.Id)
                .FirstAsync();

            settings.ActiveProfileId = other;
        }

        db.JobProfiles.Remove(profile);
        await db.SaveChangesAsync();

        return new DeleteProfileResponse(true, null);
    }
}
