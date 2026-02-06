using JobTracker.Application.Infrastructure.RPC;

namespace JobTracker.Application.Features.Tags.CreateTag;

public sealed class CreateTagHandler
    : RpcHandler<CreateTagRequest, CreateTagResponse>
{
    private readonly CreateTag _createTag;
    public override string Command => "tags.createTag";

    public CreateTagHandler(CreateTag createTag)
    {
        _createTag = createTag;
    }

    protected override async Task<CreateTagResponse> HandleAsync(CreateTagRequest request)
    {
        return await _createTag.ExecuteAsync(request);
    }
}
