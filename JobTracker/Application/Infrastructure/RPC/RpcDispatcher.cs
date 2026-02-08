using System.Text.Json;

namespace JobTracker.Application.Infrastructure.RPC;

public sealed class RpcDispatcher
{
    private readonly Dictionary<string, IRpcHandler> _handlers;

    public RpcDispatcher(IEnumerable<IRpcHandler> handlers)
    {
        _handlers = handlers.ToDictionary(
            h => h.Command,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public async Task<string> DispatchAsync(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("command", out var commandProp))
            throw new InvalidOperationException("Missing 'command'");

        if (!root.TryGetProperty("payload", out var payloadProp))
            payloadProp = default;

        var command = commandProp.GetString()
            ?? throw new InvalidOperationException("Invalid command");

        var id = root.TryGetProperty("id", out var idProp)
            ? idProp.GetString() ?? ""
            : "";

        if (!_handlers.TryGetValue(command, out var handler))
            throw new InvalidOperationException($"Unknown command: {command}");

        var result = await handler.HandleAsync(payloadProp, id);

        return JsonSerializer.Serialize(new
        {
            id = id,
            success = true,
            data = result
        });
    }
}
