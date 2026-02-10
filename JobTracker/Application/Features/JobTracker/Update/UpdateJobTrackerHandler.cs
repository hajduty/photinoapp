using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.JobTracker.Update;

[ExportTsInterface]
public record UpdateJobTrackerRequest(int TrackerId, string Keyword, string Source, string Location, bool IsActive, List<Tag> Tags, DateTime LastCheckedAt, int CheckIntervalHours = 1);

[ExportTsInterface]
public record UpdateJobTrackerResponse(JobTracker UpdatedAlert);

public sealed class UpdateJobTrackerHandler : RpcHandler<UpdateJobTrackerRequest, UpdateJobTrackerResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobTracker.updateTracker";

    public UpdateJobTrackerHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdateJobTrackerResponse> HandleAsync(UpdateJobTrackerRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        // Load the job alert with its existing tags
        var alert = await dbContext.JobTrackers
            .Include(j => j.Tags)
            .FirstOrDefaultAsync(j => j.Id == request.TrackerId);

        if (alert == null)
        {
            throw new KeyNotFoundException($"Job alert with ID {request.TrackerId} not found.");
        }

        alert.Keyword = request.Keyword;
        alert.CheckIntervalHours = request.CheckIntervalHours;
        alert.Source = request.Source;
        alert.Location = request.Location;
        alert.IsActive = request.IsActive;
        alert.LastCheckedAt = request.LastCheckedAt;

        // Clear existing tags and load new ones from database
        alert.Tags.Clear();

        if (request.Tags.Count > 0)
        {
            var tagIds = request.Tags.Select(t => t.Id).ToList();
            var existingTags = await dbContext.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();

            foreach (var tag in existingTags)
            {
                alert.Tags.Add(tag);
            }
        }

        await dbContext.SaveChangesAsync();
        return new UpdateJobTrackerResponse(alert);
    }
}
