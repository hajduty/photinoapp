using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication.DeleteApplication;

public record DeleteApplicationRequest(int JobId);
public record DeleteApplicationResponse(bool Success);

public class DeleteApplicationHandler
    : RpcHandler<DeleteApplicationRequest, DeleteApplicationResponse>
{
    public override string Command => "applications.delete";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DeleteApplicationHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<DeleteApplicationResponse> HandleAsync(DeleteApplicationRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new DeleteApplicationResponse(false);

        var application = await db.JobApplications
            .FirstOrDefaultAsync(a => a.ProfileId == settings.ActiveProfileId.Value && a.JobId == request.JobId);

        if (application == null)
            throw new InvalidOperationException($"Application for JobId {request.JobId} not found");

        db.Remove(application);
        var affected = await db.SaveChangesAsync();

        return new DeleteApplicationResponse(affected > 0);
    }
}
