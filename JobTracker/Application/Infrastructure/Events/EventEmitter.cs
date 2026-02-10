using Photino.NET;
using System.Text.Json;

namespace JobTracker.Application.Infrastructure.Events;

public sealed class EventEmitter : IEventEmitter
{
    private PhotinoWindow? _window;

    public void RegisterWindow(PhotinoWindow window)
    {
        _window = window;
    }

    public void Emit(string eventName)
    {
        Emit<object?>(eventName, null);
    }

    public void Emit<T>(string eventName, T data)
    {
        if (_window is null)
            return;

        var message = JsonSerializer.Serialize(new
        {
            type = "event",
            name = eventName,
            data,
            timestamp = DateTime.UtcNow
        });

        _window.SendWebMessage(message);
    }
}
