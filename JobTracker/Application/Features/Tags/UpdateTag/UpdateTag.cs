using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Tags.UpdateTag;

[ExportTsInterface]
public record UpdateTagRequest(int TagId, string NewName, string NewColor);

[ExportTsInterface]
public record UpdateTagResponse(Tag UpdatedTag);

public class UpdateTag
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UpdateTag(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<UpdateTagResponse> ExecuteAsync(UpdateTagRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var tag = await dbContext.Tags.FindAsync(request.TagId);
        if (tag == null)
        {
            throw new KeyNotFoundException($"Tag with ID {request.TagId} not found.");
        }
        tag.Name = request.NewName;
        tag.Color = request.NewColor;
        dbContext.Tags.Update(tag);
        await dbContext.SaveChangesAsync();
        return new UpdateTagResponse(tag);
    }
}
