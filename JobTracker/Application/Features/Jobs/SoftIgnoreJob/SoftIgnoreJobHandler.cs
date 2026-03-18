using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Jobs.SoftIgnoreJob;

public record SoftIgnoreJobRequest(int JobId);
public record SoftIgnoreJobResponse(bool Success);

public class SoftIgnoreJobHandler : RpcHandler<SoftIgnoreJobRequest, SoftIgnoreJobResponse>
{
    public override string Command => "jobs.softIgnore";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SoftIgnoreJobHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<SoftIgnoreJobResponse> HandleAsync(SoftIgnoreJobRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var rows = await db.Postings
            .Where(p => p.Id == request.JobId)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.SoftIgnore, x => x.SoftIgnore == true ? false : true));

        return new SoftIgnoreJobResponse(rows > 0);
    }
}
