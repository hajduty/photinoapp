using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Tags.CreateTag;

[ExportTsInterface]
public record CreateTagRequest(string Name, string Color);

[ExportTsInterface]
public record CreateTagResponse(Tag CreatedTag);

public class CreateTag
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CreateTag(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<CreateTagResponse> ExecuteAsync(CreateTagRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var tag = new Tag
        {
            Name = request.Name,
            Color = request.Color,
        };
        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync();

        return new CreateTagResponse(tag);
    }
}
