using JobTracker.Application.Features.JobAlerts;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace JobTracker.Application.Services;

public class JobAlertService
{
    private readonly Func<AppDbContext> _dbFactory;

    public JobAlertService(Func<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> CreateJobAlert(JobAlert alert)
    {
        using var dbContext = _dbFactory();
        await dbContext.AddAsync(alert);
        return await dbContext.SaveChangesAsync();
    }

    public async Task<List<JobAlert>> GetJobAlerts()
    {
        using var dbContext = _dbFactory();
        return await dbContext.Set<JobAlert>().ToListAsync();
    }

    public async Task<int> DeleteJobAlert(int id)
    {
        using var dbContext = _dbFactory();

        var alert = await dbContext.Set<JobAlert>().FindAsync(id);
        if (alert is null)
            return 0;

        dbContext.Remove(alert);
        return await dbContext.SaveChangesAsync();
    }
}
