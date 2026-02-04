using JobTracker.Application.Infrastructure.RPC;

namespace JobTracker.Application.Features.Tags.UpdateTag;

public sealed class UpdateTagHandler
    : RpcHandler<UpdateTagRequest, UpdateTagResponse>
{
    private readonly UpdateTag _updateTag;
    public override string Command => "tags.updateTag";

    public UpdateTagHandler(UpdateTag updateTag)
    {
        _updateTag = updateTag;
    }

    protected override async Task<UpdateTagResponse> HandleAsync(UpdateTagRequest request)
    {
        return await _updateTag.ExecuteAsync(request);
    }
}
