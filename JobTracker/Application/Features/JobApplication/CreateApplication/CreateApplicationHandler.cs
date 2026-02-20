using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication.CreateApplication;

public record CreateApplicationRequest(int JobId, string CoverLetter);
public record CreateApplicationResponse(JobApplication JobApplication);

public class CreateApplicationHandler : RpcHandler<CreateApplicationRequest, CreateApplicationResponse>
{
    public override string Command => "jobApplication.create";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CreateApplicationHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<CreateApplicationResponse> HandleAsync(CreateApplicationRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var newApplication = new JobApplication
        {
            AppliedAt = DateTime.Now,
            JobId = request.JobId,
            CoverLetter = request.CoverLetter,
            Status = ApplicationStatus.Submitted,
            LastStatusChangeAt = DateTime.Now
        };

        await db.SaveChangesAsync();

        return new CreateApplicationResponse(newApplication);
    }
}
