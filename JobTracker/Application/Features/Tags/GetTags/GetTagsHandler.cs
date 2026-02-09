using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Tags.GetTags;

public sealed class GetTagsHandler : RpcHandler<object?, List<Tag>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "tags.getTags";

    public GetTagsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<List<Tag>> HandleAsync(object? request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        return await dbContext.Tags.ToListAsync();
    }
}
