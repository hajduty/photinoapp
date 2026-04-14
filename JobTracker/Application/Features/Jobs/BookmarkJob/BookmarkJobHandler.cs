using JobTracker.Application.Features.Jobs;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobSearch.BookmarkJob;

public record BookmarkJobRequest(int PostingId, bool IsBookmarked);
public record BookmarkJobResponse(bool Success);

public class BookmarkJobHandler : RpcHandler<BookmarkJobRequest, BookmarkJobResponse>
{
    public override string Command => "jobs.bookmark";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BookmarkJobHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<BookmarkJobResponse> HandleAsync(BookmarkJobRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.ActiveProfileId == null)
            return new BookmarkJobResponse(false);

        var profileId = settings.ActiveProfileId.Value;

        var existing = await db.ProfileBookmarkedJobs
            .FirstOrDefaultAsync(b => b.ProfileId == profileId && b.PostingId == request.PostingId);

        if (request.IsBookmarked && existing == null)
        {
            db.ProfileBookmarkedJobs.Add(new ProfileBookmarkedJob
            {
                ProfileId = profileId,
                PostingId = request.PostingId,
            });
        }
        else if (!request.IsBookmarked && existing != null)
        {
            db.ProfileBookmarkedJobs.Remove(existing);
        }

        await db.SaveChangesAsync();
        return new BookmarkJobResponse(true);
    }
}
