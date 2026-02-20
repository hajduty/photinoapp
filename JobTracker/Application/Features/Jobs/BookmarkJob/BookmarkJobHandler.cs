using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobSearch.BookmarkJob;

public record BookmarkJobRequest(int PostingId, bool IsBookmarked);
public record BookmarkJobResponse(Posting Posting);
public class BookmarkJobHandler 
    : RpcHandler<BookmarkJobRequest, BookmarkJobResponse>
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

        var posting = await db.Postings.FindAsync(request.PostingId);

        if (posting == null)
        {
            throw new KeyNotFoundException($"Job alert with ID {request.PostingId} not found.");
        }

        posting.Bookmarked = request.IsBookmarked;

        await db.SaveChangesAsync();

        return new BookmarkJobResponse(posting);
    }
}
