/*using JobTracker.Application.Infrastructure.RPC;

namespace JobTracker.Application.Features.JobSearch.GetJobs;

public sealed class GetJobsHandler
    : RpcHandler<GetJobsRequest, GetJobsResponse>
{
    private readonly GetJobs _feature;
    public override string Command => "jobSearch.getJobs";

    public GetJobsHandler(GetJobs feature)
    {
        _feature = feature;
    }

    protected override async Task<GetJobsResponse> HandleAsync(GetJobsRequest request)
    {
        return await _feature.ExecuteAsync(request);
    }
}
*/