using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JobTracker.Application.Features.Tags.CreateTag;

public record CreateTagRequest(string Name, string Color);
public record CreateTagResponse(Tag CreatedTag);

public sealed class CreateTagHandler
    : RpcHandler<CreateTagRequest, CreateTagResponse>
{
    public override string Command => "tags.createTag";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CreateTagHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<CreateTagResponse> HandleAsync(CreateTagRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var tag = new Tag
        {
            Name = request.Name,
            Color = request.Color,
        };

        Debug.WriteLine(request.Name);
        dbContext.Tags.Add(tag);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            throw;
        }

        return new CreateTagResponse(tag);
    }
}
