using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Infrastructure.RPC;

namespace JobTracker.Application.Features.JobSearch.GetJobTitles;

public sealed class GetJobTitlesHandler
        : RpcHandler<GetJobTitlesRequest, GetJobTitlesResponse>
{
    public override string Command => "jobSearch.getTitles";

    private readonly GetJobTitles _feature;

    public GetJobTitlesHandler(GetJobTitles feature)
    {
        _feature = feature;
    }

    protected override async Task<GetJobTitlesResponse> HandleAsync(GetJobTitlesRequest request)
    {
        return await _feature.ExecuteAsync(request);
    }
}
