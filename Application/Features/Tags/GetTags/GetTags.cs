using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Tags.GetTags;

public class GetTags
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetTags(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Tag>> ExecuteAsync()
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        return await dbContext.Tags.ToListAsync();
    }
}
