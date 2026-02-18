using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Tags.DeleteTag;

public record DeleteTagRequest(int TagId);

public record DeleteTagResponse(bool Success);

public sealed class DeleteTagHandler : RpcHandler<DeleteTagRequest, DeleteTagResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "tags.deleteTag";

    public DeleteTagHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<DeleteTagResponse> HandleAsync(DeleteTagRequest request)
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
