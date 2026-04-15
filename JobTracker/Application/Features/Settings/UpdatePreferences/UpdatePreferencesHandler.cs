using JobTracker.Application.Features.Profiles;
using JobTracker.Application.Features.Settings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

[ExportTsInterface]
public record UpdatePreferencesRequest(
    string? UserCV,
    List<int>? SelectedTagIds,
    int? YearsOfExperience,
    List<KeywordRule>? BlockedKeywords,
    List<KeywordRule>? MatchedKeywords,
    bool? AlertOnAllMatchingJobs,
    bool? AlertOnHardMatchingJobs,
    List<string>? Locations,
    int? MaxJobAgeDays
);

[ExportTsInterface]
public record UpdatePreferencesResponse(JobProfile Profile);

public class UpdatePreferencesHandler : RpcHandler<UpdatePreferencesRequest, UpdatePreferencesResponse>
{
    public override string Command => "settings.updatePreferences";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UpdatePreferencesHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdatePreferencesResponse> HandleAsync(UpdatePreferencesRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            throw new InvalidOperationException("No active profile found.");

        var profile = await db.JobProfiles
            .Include(p => p.SelectedTags)
            .FirstAsync(p => p.Id == settings.ActiveProfileId);

        profile.UserCV = request.UserCV;
        profile.YearsOfExperience = request.YearsOfExperience;
        profile.BlockedKeywords = request.BlockedKeywords;
        profile.MatchedKeywords = request.MatchedKeywords;
        profile.AlertOnAllMatchingJobs = request.AlertOnAllMatchingJobs;
        profile.AlertOnHardMatchingJobs = request.AlertOnHardMatchingJobs;
        profile.Locations = request.Locations;
        profile.MaxJobAgeDays = request.MaxJobAgeDays;
        profile.UserEmbedding = null; // invalidate cached embedding

        if (request.SelectedTagIds != null)
        {
            var tags = await db.Tags
                .Where(t => request.SelectedTagIds.Contains(t.Id))
                .ToListAsync();

            profile.SelectedTags ??= [];
            profile.SelectedTags.Clear();

            foreach (var tag in tags)
                profile.SelectedTags.Add(tag);
        }

        await db.SaveChangesAsync();

        return new UpdatePreferencesResponse(profile);
    }
}
