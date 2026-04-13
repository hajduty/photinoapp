using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.System.Settings.RejectedTechKeywords;

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

        var settings = await db.Settings
            .FirstOrDefaultAsync();

        if (settings == null || settings.RejectedTechKeywords == null)
            return new RemoveRejectedTechKeywordResponse(false);

        var removed = settings.RejectedTechKeywords.Remove(request.Id); 

        await db.SaveChangesAsync();

        return new RemoveRejectedTechKeywordResponse(removed);
    }
}