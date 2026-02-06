using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Tags.DeleteTag;

[ExportTsInterface]
public record DeleteTagRequest(int TagId);

[ExportTsInterface]
public record DeleteTagResponse(bool Success);

public class DeleteTag
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DeleteTag(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DeleteTagResponse> ExecuteAsync(DeleteTagRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var tag = await dbContext.Tags.FindAsync(request.TagId);
        if (tag == null)
        {
            return new DeleteTagResponse(false);
        }

        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync();
        return new DeleteTagResponse(true);
    }
}
