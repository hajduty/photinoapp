using JobTracker.Application.Infrastructure.RPC;
using System.Diagnostics;

namespace JobTracker.Application.Features.Tags.GetTags;

public sealed class GetTagsHandler
    : RpcHandler<object?, List<Tag>>
{
    private readonly GetTags _getTags;
    public override string Command => "tags.getTags";

    public GetTagsHandler(GetTags getTags)
    {
        _getTags = getTags;
    }

    protected override async Task<List<Tag>> HandleAsync(object? request)
    {
        return await _getTags.ExecuteAsync();
    }
}