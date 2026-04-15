using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Profiles.CreateProfile;

public record CreateProfileRequest(string Name);
public record CreateProfileResponse(JobProfile Profile, bool Success);

public class CreateProfileHandler : RpcHandler<CreateProfileRequest, CreateProfileResponse>
{
    public override string Command => "profiles.create";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CreateProfileHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<CreateProfileResponse> HandleAsync(CreateProfileRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var profile = new JobProfile
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "New Profile" : request.Name.Trim(),
        };

        db.JobProfiles.Add(profile);
        await db.SaveChangesAsync();

        return new CreateProfileResponse(profile, true);
    }
}
