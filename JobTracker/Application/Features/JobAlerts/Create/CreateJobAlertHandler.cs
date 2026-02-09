using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.JobAlerts.Create;

public record CreateJobAlertRequest(string Keyword);
public record CreateJobAlertResponse(int AlertId);

public class CreateJobAlertHandler : RpcHandler<CreateJobAlertRequest, CreateJobAlertResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobAlerts.createAlert";

    public CreateJobAlertHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<CreateJobAlertResponse> HandleAsync(CreateJobAlertRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var jobAlert = new JobAlert { Keyword = request.Keyword };

        dbContext.JobAlerts.Add(jobAlert);
        await dbContext.SaveChangesAsync();
        return new CreateJobAlertResponse(jobAlert.Id);
    }
}
