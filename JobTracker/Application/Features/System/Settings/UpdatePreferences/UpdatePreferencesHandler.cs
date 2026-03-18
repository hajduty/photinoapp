using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

[ExportTsInterface]
public record UpdatePreferencesRequest(
    string? UserCV,
    List<int>? SelectedTagIds,
    int? YearsOfExperience,
    List<string>? BlockedKeywords,
    List<string>? MatchedKeywords,
    bool? AlertOnAllMatchingJobs,
    bool? AlertOnHardMatchingJobs,
    string? Location,
    int? MaxJobAgeDays
);

[ExportTsInterface]
public record UpdatePreferencesResponse(Settings Settings);

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

        var settings = await db.Settings
            .Include(s => s.SelectedTags)
            .FirstAsync();

        settings.UserCV = request.UserCV;
        settings.YearsOfExperience = request.YearsOfExperience;
        settings.BlockedKeywords = request.BlockedKeywords;
        settings.MatchedKeywords = request.MatchedKeywords;
        settings.AlertOnAllMatchingJobs = request.AlertOnAllMatchingJobs;
        settings.AlertOnHardMatchingJobs = request.AlertOnHardMatchingJobs;
        settings.Location = request.Location;
        settings.MaxJobAgeDays = request.MaxJobAgeDays;
        settings.UserEmbedding = null;

        if (request.SelectedTagIds != null)
        {
            var tags = await db.Tags
                .Where(t => request.SelectedTagIds.Contains(t.Id))
                .ToListAsync();

            settings.SelectedTags.Clear();

            foreach (var tag in tags)
                settings.SelectedTags.Add(tag);
        }

        settings.LastUpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new UpdatePreferencesResponse(settings);
    }
}