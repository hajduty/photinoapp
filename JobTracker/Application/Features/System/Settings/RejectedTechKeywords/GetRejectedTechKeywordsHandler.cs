using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.System.Settings.RejectedTechKeywords;

public record RejectedTagEntry(int TagId, string TagName, string TagColor, KeywordScope Scope);
public record GetRejectedTechKeywordsRequest();
public record GetRejectedTechKeywordsResponse(List<RejectedTagEntry> RejectedKeywords);

public class GetRejectedTechKeywordsHandler : RpcHandler<GetRejectedTechKeywordsRequest, GetRejectedTechKeywordsResponse>
{
    public override string Command => "settings.getRejectedTechKeywords";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetRejectedTechKeywordsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetRejectedTechKeywordsResponse> HandleAsync(GetRejectedTechKeywordsRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new GetRejectedTechKeywordsResponse([]);

        var profile = await db.JobProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == settings.ActiveProfileId);

        if (profile?.RejectedTechKeywords == null || profile.RejectedTechKeywords.Count == 0)
            return new GetRejectedTechKeywordsResponse([]);

        var tagIds = profile.RejectedTechKeywords.Select(r => r.TagId).ToList();
        var tags = await db.Tags
            .AsNoTracking()
            .Where(t => tagIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        var result = profile.RejectedTechKeywords
            .Where(r => tags.ContainsKey(r.TagId))
            .Select(r => new RejectedTagEntry(r.TagId, tags[r.TagId].Name, tags[r.TagId].Color, r.Scope))
            .ToList();

        return new GetRejectedTechKeywordsResponse(result);
    }
}
