using System.Text.Json;

namespace JobTracker.Application.Infrastructure.RPC;

public sealed record NoRequest;

public abstract class RpcHandler<TRequest, TResponse> : IRpcHandler
{
    public abstract string Command { get; }

    public async Task<object?> HandleAsync(JsonElement payload, string id)
    {
        var request = payload.Deserialize<TRequest>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new InvalidOperationException("Invalid RPC payload");

        return await HandleAsync(request);
    }

    protected abstract Task<TResponse> HandleAsync(TRequest request);
}