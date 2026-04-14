using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.System.Profiles.SwitchProfile;

public record SwitchProfileRequest(int ProfileId);
public record SwitchProfileResponse(bool Success, int ActiveProfileId);

public class SwitchProfileHandler : RpcHandler<SwitchProfileRequest, SwitchProfileResponse>
{
    public override string Command => "profiles.switch";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SwitchProfileHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<SwitchProfileResponse> HandleAsync(SwitchProfileRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var profileExists = await db.JobProfiles.AnyAsync(p => p.Id == request.ProfileId);
        if (!profileExists)
            return new SwitchProfileResponse(false, request.ProfileId);

        var settings = await db.Settings.FirstOrDefaultAsync();
        if (settings == null)
            return new SwitchProfileResponse(false, request.ProfileId);

        settings.ActiveProfileId = request.ProfileId;
        await db.SaveChangesAsync();

        return new SwitchProfileResponse(true, request.ProfileId);
    }
}
