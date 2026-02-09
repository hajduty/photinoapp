using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.JobAlerts.Update;

[ExportTsInterface]
public record UpdateJobAlertRequest(int AlertId, string NewKeyword, int NewCheckIntervalHours);

[ExportTsInterface]
public record UpdateJobAlertResponse(JobAlert UpdatedAlert);

public sealed class UpdateJobAlertHandler : RpcHandler<UpdateJobAlertRequest, UpdateJobAlertResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobAlerts.updateAlert";

    public UpdateJobAlertHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdateJobAlertResponse> HandleAsync(UpdateJobAlertRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var alert = await dbContext.JobAlerts.FindAsync(request.AlertId);
        if (alert == null)
        {
            throw new KeyNotFoundException($"Job alert with ID {request.AlertId} not found.");
        }
        alert.Keyword = request.NewKeyword;
        alert.CheckIntervalHours = request.NewCheckIntervalHours;
        dbContext.JobAlerts.Update(alert);
        await dbContext.SaveChangesAsync();
        return new UpdateJobAlertResponse(alert);
    }
}
