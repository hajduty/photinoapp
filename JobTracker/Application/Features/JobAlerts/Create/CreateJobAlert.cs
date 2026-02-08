using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobAlerts.Create;


public record CreateJobAlertRequest(string Keyword);
public record CreateJobAlertResponse(int AlertId);

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
        var jobAlert = new JobAlert { Keyword = request.Keyword };

        dbContext.JobAlerts.Add(jobAlert);

        await dbContext.SaveChangesAsync();
        return new CreateJobAlertResponse(jobAlert.Id);
    }
}
