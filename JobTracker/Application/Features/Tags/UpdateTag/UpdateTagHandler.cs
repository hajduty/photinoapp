using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Tags.UpdateTag;

public record UpdateTagRequest(int TagId, string NewName, string NewColor);
public record UpdateTagResponse(Tag UpdatedTag);

public sealed class UpdateTagHandler : RpcHandler<UpdateTagRequest, UpdateTagResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "tags.updateTag";

    public UpdateTagHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdateTagResponse> HandleAsync(UpdateTagRequest request)
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
