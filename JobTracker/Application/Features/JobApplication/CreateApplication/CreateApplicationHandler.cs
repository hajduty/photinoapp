using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobApplication.CreateApplication;

public record CreateApplicationRequest(int JobId, string CoverLetter);
public record CreateApplicationResponse(JobApplication JobApplication);

public class CreateApplicationHandler : RpcHandler<CreateApplicationRequest, CreateApplicationResponse>
{
    public override string Command => "applications.create";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CreateApplicationHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<CreateApplicationResponse> HandleAsync(CreateApplicationRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            throw new InvalidOperationException("No active profile found");

        var posting = await db.Postings.FindAsync(request.JobId);

        if (posting == null)
            throw new InvalidOperationException("Posting not found");

        var newApplication = new JobApplication
        {
            ProfileId = settings.ActiveProfileId.Value,
            AppliedAt = DateTime.UtcNow,
            JobId = posting.Id,
            Posting = posting,
            CoverLetter = request.CoverLetter,
            Status = ApplicationStatus.Pending,
            LastStatusChangeAt = DateTime.UtcNow,
            StatusHistory = new List<ApplicationStatusHistory>
            {
                new()
                {
                    Status = ApplicationStatus.Pending,
                    ChangedAt = DateTime.UtcNow,
                    Note = "Application created"
                }
            }
        };

        db.JobApplications.Add(newApplication);

        await db.SaveChangesAsync();

        return new CreateApplicationResponse(newApplication);
    }
}
