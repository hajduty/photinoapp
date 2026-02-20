using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication;

public record UpdateApplicationRequest(int Id, ApplicationStatus Status);
public record UpdateApplicationResponse(JobApplication Application);

public class UpdateApplication 
    : RpcHandler<UpdateApplicationRequest, UpdateApplicationResponse>
{
    public override string Command => "jobApplication.update";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UpdateApplication(IDbContextFactory<AppDbContext> dbFactory)
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

        application.Status = request.Status;
        application.LastStatusChangeAt = DateTime.Now;
        await db.SaveChangesAsync();

        return new UpdateApplicationResponse(application);
    }
}
