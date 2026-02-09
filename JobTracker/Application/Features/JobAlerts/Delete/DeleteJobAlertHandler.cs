using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.JobAlerts.Delete;

[ExportTsInterface]
public record DeleteJobAlertRequest(int AlertId);

[ExportTsInterface]
public record DeleteJobAlertResponse(bool Success);

public sealed class DeleteJobAlertHandler : RpcHandler<DeleteJobAlertRequest, DeleteJobAlertResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobAlerts.deleteAlert";

    public DeleteJobAlertHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<DeleteJobAlertResponse> HandleAsync(DeleteJobAlertRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var alert = await dbContext.JobAlerts.FindAsync(request.AlertId);
        if (alert == null)
        {
            return new DeleteJobAlertResponse(false);
        }

        dbContext.JobAlerts.Remove(alert);
        await dbContext.SaveChangesAsync();
        return new DeleteJobAlertResponse(true);
    }
}
