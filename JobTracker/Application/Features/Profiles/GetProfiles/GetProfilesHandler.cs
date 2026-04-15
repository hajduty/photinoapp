using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Profiles.GetProfiles;

public record GetProfilesResponse(List<JobProfile> Profiles, int? ActiveProfileId);

public class GetProfilesHandler : RpcHandler<NoRequest, GetProfilesResponse>
{
    public override string Command => "profiles.getProfiles";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetProfilesHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetProfilesResponse> HandleAsync(NoRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        var profiles = await db.JobProfiles.Include(p => p.SelectedTags).AsNoTracking().ToListAsync();

        return new GetProfilesResponse(profiles, settings?.ActiveProfileId);
    }
}
