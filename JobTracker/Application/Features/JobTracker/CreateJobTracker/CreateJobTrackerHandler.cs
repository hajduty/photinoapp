using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobTracker.CreateJobTracker;

public record CreateJobTrackerRequest(string Keyword, string Source, string Location, bool IsActive, List<Tag> Tags, DateTime LastCheckedAt, int CheckIntervalHours = 1);

public record CreateJobTrackerResponse(JobTracker jobAlert);

public class CreateJobTrackerHandler : RpcHandler<CreateJobTrackerRequest, CreateJobTrackerResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobTracker.createTracker";

    public CreateJobTrackerHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<CreateJobTrackerResponse> HandleAsync(CreateJobTrackerRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var settings = await dbContext.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            throw new InvalidOperationException("No active profile found");

        // Get the IDs of tags from the request
        var tagIds = request.Tags.Select(t => t.Id).ToList();

        // Query existing tags from the database (tracked entities)
        var existingTags = await dbContext.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync();

        var jobAlert = new JobTracker
        {
            ProfileId = settings.ActiveProfileId.Value,
            Keyword = request.Keyword,
            Source = request.Source,
            Location = request.Location,
            IsActive = request.IsActive,
            LastCheckedAt = request.LastCheckedAt,
            CheckIntervalHours = request.CheckIntervalHours,
            Tags = existingTags
        };

        dbContext.JobTrackers.Add(jobAlert);
        await dbContext.SaveChangesAsync();

        // Reload with Tags included to return complete data
        var createdAlert = await dbContext.JobTrackers
            .Include(j => j.Tags)
            .FirstAsync(j => j.Id == jobAlert.Id);

        return new CreateJobTrackerResponse(createdAlert);
    }
}
