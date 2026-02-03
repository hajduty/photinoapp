using System.Text.Json;

namespace JobTracker.Application.Infrastructure.RPC;

public interface IRpcHandler
{
    string Command { get; }
    Task<object?> HandleAsync(JsonElement payload);
}