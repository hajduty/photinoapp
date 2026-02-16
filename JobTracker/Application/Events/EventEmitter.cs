using Photino.NET;
using System.Text.Json;

namespace JobTracker.Application.Events;

public sealed class EventEmitter : IEventEmitter
{
    private readonly object _lock = new object();
    private PhotinoWindow? _window;

    public void RegisterWindow(PhotinoWindow window)
    {
        lock (_lock)
        {
            _window = window;
        }
    }

    public void Emit(string eventName)
    {
        Emit<object?>(eventName, null);
    }

    public void Emit<T>(string eventName, T data)
    {
        PhotinoWindow? currentWindow;

        lock (_lock)
        {
            currentWindow = _window;
        }

        if (currentWindow is null)
            return;

        try
        {
            var message = JsonSerializer.Serialize(new
            {
                type = "event",
                name = eventName,
                data,
                timestamp = DateTime.UtcNow
            });

            currentWindow.SendWebMessage(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EventEmitter error: {ex.Message}");
        }
    }
}