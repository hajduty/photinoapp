using JobTracker.Application.Infrastructure.RPC;

namespace JobTracker.Application.Features.JobSearch.LoadJobs;

public class LoadJobsHandler
    : RpcHandler<LoadJobsRequest, LoadJobsResponse>
{
    private readonly LoadJobs _feature;
    public override string Command => "jobSearch.loadJobs";
    public LoadJobsHandler(LoadJobs feature)
    {
        _feature = feature;
    }

    protected override async Task<LoadJobsResponse> HandleAsync(LoadJobsRequest request)
    {
        return await _feature.ExecuteAsync(request);
    }
}
