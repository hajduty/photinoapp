using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobAlerts.Create;


public record CreateJobAlertRequest(string keyword, string location);
public record CreateJobAlertResponse(int alertId);

public class CreateJobAlert
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CreateJobAlert(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<CreateJobAlertResponse> ExecuteAsync(CreateJobAlertRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var jobAlert = new JobAlert
        {
            Keyword = request.keyword,
            Location = request.location,
        };
        dbContext.JobAlerts.Add(jobAlert);

        await dbContext.SaveChangesAsync();
        return new CreateJobAlertResponse(jobAlert.Id);
    }
}
