using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;

namespace JobTracker.Application.Features.JobTracker.Process;

public record ProcessJobTrackerResponse(int JobsAdded);

public class ProcessJobTrackerHandler 
    : RpcHandler<object?, ProcessJobTrackerResponse>
{
    private readonly TrackerService _trackerService;
    public override string Command => "jobTracker.process";

    public ProcessJobTrackerHandler(TrackerService trackerservice)
    {
        _trackerService = trackerservice;
    }

    protected async override Task<ProcessJobTrackerResponse> HandleAsync(object? request)
    {
        return new ProcessJobTrackerResponse(await _trackerService.Run());
    }
}
