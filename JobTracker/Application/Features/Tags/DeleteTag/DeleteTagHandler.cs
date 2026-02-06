using JobTracker.Application.Infrastructure.RPC;

namespace JobTracker.Application.Features.Tags.DeleteTag;

public sealed class DeleteTagHandler
    : RpcHandler<DeleteTagRequest, DeleteTagResponse>
{
    private readonly DeleteTag _deleteTag;
    public override string Command => "tags.deleteTag";
    public DeleteTagHandler(DeleteTag deleteTag)
    {
        _deleteTag = deleteTag;
    }

    protected override async Task<DeleteTagResponse> HandleAsync(DeleteTagRequest request)
    {
        return await _deleteTag.ExecuteAsync(request);
    }
}