using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;

namespace JobTracker.Application.Features.JobTracker.Process;

public record ProcessJobTrackerResponse(int JobsAdded);

public class ProcessJobTrackerHandler 
    : RpcHandler<object?, ProcessJobTrackerResponse>
{
    private readonly TrackerService _trackerService;
    private readonly EmbeddingProcessor _embeddingProcessor;
    public override string Command => "jobTracker.process";

    public ProcessJobTrackerHandler(TrackerService trackerservice, EmbeddingProcessor embeddingProcessor)
    {
        _trackerService = trackerservice;
        _embeddingProcessor = embeddingProcessor;
    }

    protected async override Task<ProcessJobTrackerResponse> HandleAsync(object? request)
    {
        int amount = await _trackerService.Run(true); 
        await _embeddingProcessor.GenerateEmbeddingsAsync();

        return new ProcessJobTrackerResponse(amount);
    }
}
