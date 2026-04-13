using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.System.Settings.RejectedTechKeywords;

public record UpdateRejectedTagScopeRequest(int TagId, KeywordScope Scope);
public record UpdateRejectedTagScopeResponse(bool Success);

public class UpdateRejectedTagScopeHandler : RpcHandler<UpdateRejectedTagScopeRequest, UpdateRejectedTagScopeResponse>
{
    public override string Command => "settings.updateRejectedTagScope";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UpdateRejectedTagScopeHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdateRejectedTagScopeResponse> HandleAsync(UpdateRejectedTagScopeRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.FirstOrDefaultAsync();

        if (settings?.RejectedTechKeywords == null)
            return new UpdateRejectedTagScopeResponse(false);

        var existing = settings.RejectedTechKeywords.FirstOrDefault(r => r.TagId == request.TagId);
        if (existing == null)
            return new UpdateRejectedTagScopeResponse(false);

        settings.RejectedTechKeywords.Remove(existing);
        settings.RejectedTechKeywords.Add(existing with { Scope = request.Scope });

        await db.SaveChangesAsync();

        return new UpdateRejectedTagScopeResponse(true);
    }
}
