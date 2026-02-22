using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication;

public record UpdateApplicationRequest(int Id, ApplicationStatus ApplicationStatus, string? Note = null);
public record UpdateApplicationResponse(JobApplication Application);

public class UpdateApplicationHandler 
    : RpcHandler<UpdateApplicationRequest, UpdateApplicationResponse>
{
    public override string Command => "applications.update";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UpdateApplicationHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdateApplicationResponse> HandleAsync(UpdateApplicationRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var application = await db.JobApplications.FindAsync(request.Id);

        if (application == null)
        {
            throw new InvalidOperationException($"Job with Id {request.Id} not found");
        }

        if (application.Status != request.ApplicationStatus)
        {
            application.Status = request.ApplicationStatus;
            application.LastStatusChangeAt = DateTime.Now;

            db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                JobApplicationId = application.JobId,
                Status = request.ApplicationStatus,
                ChangedAt = DateTime.Now,
                Note = request.Note
            });
        }

        await db.SaveChangesAsync();

        return new UpdateApplicationResponse(application);
    }
}
