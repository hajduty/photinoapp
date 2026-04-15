using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Settings.RejectedTechKeywords;

public record RemoveRejectedTechKeywordRequest(int Id);
public record RemoveRejectedTechKeywordResponse(bool Success);

public class RemoveRejectedTechKeywordHandler : RpcHandler<RemoveRejectedTechKeywordRequest, RemoveRejectedTechKeywordResponse>
{
    public override string Command => "settings.removeRejectedTechKeyword";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RemoveRejectedTechKeywordHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<RemoveRejectedTechKeywordResponse> HandleAsync(RemoveRejectedTechKeywordRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new RemoveRejectedTechKeywordResponse(false);

        var profile = await db.JobProfiles
            .FirstOrDefaultAsync(p => p.Id == settings.ActiveProfileId);

        if (profile?.RejectedTechKeywords == null)
            return new RemoveRejectedTechKeywordResponse(false);

        var removed = profile.RejectedTechKeywords.RemoveAll(r => r.TagId == request.Id) > 0;
        await db.SaveChangesAsync();

        return new RemoveRejectedTechKeywordResponse(removed);
    }
}
