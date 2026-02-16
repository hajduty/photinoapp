using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobTracker.Delete;

public record DeleteJobTrackerRequest(int TrackerId);

public record DeleteJobTrackerResponse(bool Success);

public sealed class DeleteJobTrackerHandler : RpcHandler<DeleteJobTrackerRequest, DeleteJobTrackerResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobTracker.deleteTracker";

    public DeleteJobTrackerHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<DeleteJobTrackerResponse> HandleAsync(DeleteJobTrackerRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var alert = await dbContext.JobTrackers.FindAsync(request.TrackerId);
        if (alert == null)
        {
            return new DeleteJobTrackerResponse(false);
        }

        dbContext.JobTrackers.Remove(alert);
        await dbContext.SaveChangesAsync();
        return new DeleteJobTrackerResponse(true);
    }
}
