using JobTracker.Application.Features.Profiles;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Settings.GetSettings;

[ExportTsInterface]
public record GetSettingsResponse(Settings Settings, JobProfile? ActiveProfile, List<JobProfile> Profiles);

public sealed class GetSettingsHandler : RpcHandler<object?, GetSettingsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "settings.getSettings";

    public GetSettingsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetSettingsResponse> HandleAsync(object? request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new Settings();
            db.Settings.Add(settings);
            await db.SaveChangesAsync();
        }

        // Ensure an active profile exists
        if (settings.ActiveProfileId == null)
        {
            var fallback = await db.JobProfiles.FirstOrDefaultAsync()
                           ?? new JobProfile { Name = "Default" };

            if (fallback.Id == 0)
            {
                db.JobProfiles.Add(fallback);
                await db.SaveChangesAsync();
            }

            settings.ActiveProfileId = fallback.Id;
            await db.SaveChangesAsync();
        }

        var profiles = await db.JobProfiles
            .Include(p => p.SelectedTags)
            .ToListAsync();

        var activeProfile = profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId);

        return new GetSettingsResponse(settings, activeProfile, profiles);
    }
}
