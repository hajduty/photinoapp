using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Jobs.IgnoreJob;

public record IgnoreJobRequest(int JobId);
public record IgnoreJobResponse(bool Success);

public class IgnoreJobHandler : RpcHandler<IgnoreJobRequest, IgnoreJobResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobs.ignore";

    public IgnoreJobHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<IgnoreJobResponse> HandleAsync(IgnoreJobRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var rows = await db.Postings
            .Where(p => p.Id == request.JobId)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.Ignored, true));

        return new IgnoreJobResponse(rows > 0);
    }
}
