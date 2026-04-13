using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.System.Settings.RejectedTechKeywords;

public record GetRejectedTechKeywordsRequest();
public record GetRejectedTechKeywordsResponse(List<Tag> RejectedKeywords);

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

        var settings = await db.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings == null || settings.RejectedTechKeywords == null || settings.RejectedTechKeywords.Count == 0)
            return new GetRejectedTechKeywordsResponse([]);

        var rejectedTags = await db.Tags
            .AsNoTracking()
            .Where(t => settings.RejectedTechKeywords.Contains(t.Id))
            .ToListAsync();

        return new GetRejectedTechKeywordsResponse(rejectedTags);
    }
}