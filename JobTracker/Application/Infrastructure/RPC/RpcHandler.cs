using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobTracker.Application.Infrastructure.RPC;

public sealed record NoRequest;

public abstract class RpcHandler<TRequest, TResponse> : IRpcHandler
{
    public abstract string Command { get; }

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<object?> HandleAsync(JsonElement payload, string id)
    {
        TRequest? request;

        if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
        {
            request = Activator.CreateInstance<TRequest>();
        }
        else
        {
            request = payload.Deserialize<TRequest>(_options);
        }

        if (request == null)
            throw new InvalidOperationException("Invalid RPC payload");

        return await HandleAsync(request);
    }

    protected abstract Task<TResponse> HandleAsync(TRequest request);
}