using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;

namespace JobTracker.Application.Features.SemanticSearch.Cancel;

public class CancelWorkerHandler : RpcHandler<object?, object?>
{
    private readonly EmbeddingService _embeddingService;

    public override string Command => "semanticSearch.cancel";

    public CancelWorkerHandler(EmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    protected async override Task<object?> HandleAsync(object? request)
    {
        _embeddingService.Cancel();

        return null;
    }
}
